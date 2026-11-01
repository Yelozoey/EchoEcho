"""Audit the Git index without printing secret values or changing any files."""
import argparse, hashlib, json, re, subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
SECRET_PATTERNS={
    'service-token':re.compile(r'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{50,}|AKIA[0-9A-Z]{16}|hf_[A-Za-z0-9]{30,}|sk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{32,})\b'),
    'private-key':re.compile(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
    'credential-url':re.compile(r'https?://[^\s/:]+:[^\s/@]+@'),
}
TEXT={'.cs','.py','.json','.md','.txt','.shader','.hlsl','.cginc','.asmdef','.meta','.unity','.prefab','.mat','.asset','.yaml','.yml','.toml','.xml','.config','.uxml','.uss'}

def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
def findings(text,path):
    rows=[]
    for number,line in enumerate(text.splitlines(),1):
        for kind,pattern in SECRET_PATTERNS.items():
            if pattern.search(line):rows.append({'path':path,'line':number,'kind':kind,'lineSha256':hashlib.sha256(line.encode()).hexdigest()})
    return rows

def audit():
    paths=git('ls-files','-z').decode('utf-8').rstrip('\0').split('\0');paths=[s for s in paths if s];tracked=set(paths)
    errors=[];secret=[];sizes=[];guids={};text_count=0
    for name in paths:
        path=ROOT/name;size=path.stat().st_size;sizes.append((size,name))
        if size>50*1024**2:errors.append({'path':name,'kind':'over-50MiB'})
        if name.startswith(('Library/','Temp/','Builds/','Logs/','UserSettings/','.codex/','.vscode/')):errors.append({'path':name,'kind':'machine-or-build-output'})
        if name.startswith('Assets/StreamingAssets/EchoLLM/') and (name.endswith('.onnx') or name.endswith('.onnx.meta') or name.endswith('.onnx_data') or name.endswith('.onnx_data.meta')):errors.append({'path':name,'kind':'weight-unexpected'})
        if name.startswith('Assets/'):
            if not name.endswith('.meta') and name+'.meta' not in tracked:errors.append({'path':name,'kind':'missing-meta'})
            if name.endswith('.meta'):
                target=name[:-5]
                if target not in tracked and not any(s.startswith(target+'/') for s in tracked):errors.append({'path':name,'kind':'orphan-meta'})
                match=re.search(r'^guid:\s*([0-9a-f]{32})',path.read_text(encoding='utf-8-sig'),re.M)
                if match:
                    value=match.group(1)
                    if value in guids:errors.append({'path':name,'kind':'duplicate-guid','other':guids[value]})
                    guids[value]=name
        if path.suffix.lower() in TEXT or path.name in {'.gitignore','.gitattributes'}:
            raw=path.read_bytes()
            if b'\0' not in raw[:4096]:
                try:text=raw.decode('utf-8-sig')
                except UnicodeDecodeError:continue
                text_count+=1;secret+=findings(text,name)
    lock=json.loads((ROOT/'Packages/packages-lock.json').read_text(encoding='utf-8'))['dependencies'];missing=[]
    deps=json.loads((ROOT/'Docs/Repository/CoatedSceneDependencies.json').read_text(encoding='utf-8'))['dependencies']
    for dep in deps:
        if dep.startswith('Assets/') and dep not in tracked:missing.append(dep)
        elif dep.startswith('Packages/') and dep not in tracked:
            package=dep.split('/')[1]
            if package not in lock or lock[package].get('source')=='embedded':missing.append(dep)
    errors += [{'path':d,'kind':'missing-main-scene-dependency'} for d in missing]
    mandatory=['AGENTS.md','HANDOFF.md','README.md','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','Docs/Repository/RebuildAndModels.md','Docs/Repository/ThirdPartyNotices.md','ThirdPartyNotices/UnityMCP_LICENSE.txt']
    errors += [{'path':d,'kind':'missing-required-file'} for d in mandatory if d not in tracked]
    checked_metadata=0
    for manifest in (ROOT/'Assets/StreamingAssets/EchoLLM').glob('*/manifest.json'):
        data=json.loads(manifest.read_text(encoding='utf-8-sig'))
        for record in data['files']:
            if record['name']=='model.onnx':continue
            name=(manifest.parent/record['name']).relative_to(ROOT).as_posix()
            if name not in tracked:errors.append({'path':name,'kind':'missing-model-metadata'});continue
            raw=git('show',':'+name);checked_metadata+=1
            if len(raw)!=record['bytes'] or hashlib.sha256(raw).hexdigest()!=record['sha256']:errors.append({'path':name,'kind':'checksummed-metadata-index-mismatch'})
    allow_path=ROOT/'Docs/Repository/AuditAllowlist.json';allow=json.loads(allow_path.read_text(encoding='utf-8')) if allow_path.exists() else [];reviewed=[];pending=[]
    for entry in secret:
        match=next((a for a in allow if all(a.get(k)==entry.get(k) for k in ['path','line','kind','lineSha256'])),None)
        if match:reviewed.append({**entry,'reason':match['reason']})
        else:pending.append(entry)
    return {'passed':not errors and not pending,'files':len(paths),'bytes':sum(s for s,_ in sizes),'textFilesScanned':text_count,'assetGuids':len(guids),'mainSceneDependencies':len(deps),'modelMetadataHashes':checked_metadata,'largest':[{'path':n,'bytes':s} for s,n in sorted(sizes,reverse=True)[:8]],'errors':errors,'secretFindings':pending,'reviewedExamples':reviewed,'scope':'Index path/size/meta/dependency and recognized-token scan; not a guarantee against every secret format or fresh-machine Unity import'}

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path);p.add_argument('--self-test',action='store_true');a=p.parse_args()
    if a.self_test:
        assert findings('gh'+'p_'+('A'*36),'synthetic')
        assert findings('-----BEGIN '+'PRIVATE KEY-----','synthetic')
        assert not findings('Use an environment variable, never store a credential.','safe')
        print('Audit detector self-checks: 3/3');raise SystemExit(0)
    result=audit()
    if a.output:
        target=a.output.resolve()
        if not target.is_relative_to(ROOT):raise SystemExit('Output outside workspace')
        if target.exists():raise SystemExit('Preserve existing audit receipt')
        target.parent.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(result,ensure_ascii=True,indent=2));raise SystemExit(0 if result['passed'] else 1)
