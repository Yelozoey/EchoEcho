"""Procedural typography, using the project's licensed Noto font; no hand painted surfaces."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
root=Path(__file__).resolve().parents[2]
out=root/'Assets/EchoStation/SalvageNext/ArtV2/Generated/Signs';out.mkdir(parents=True,exist_ok=True)
font=str(root/'Assets/EchoStation/ThirdParty/Noto/Resources/NotoSansCJKsc-Regular.otf')
for i,(title,sub,accent) in enumerate([('RECOVERY BAY','回收泊位  /  交付 · 充电',(94,208,219)),('STORAGE','储藏区  /  物资整理',(143,172,213)),('MAINTENANCE','检修间  /  设备检查',(236,159,83))]):
 p=out/f'Zone_{i}.png'
 if p.exists():raise FileExistsError(p)
 im=Image.new('RGB',(1024,256),(18,29,40));d=ImageDraw.Draw(im)
 d.rectangle((0,0,14,255),fill=accent);d.rectangle((40,212,984,215),fill=accent)
 d.text((38,6),f'0{i+1}',font=ImageFont.truetype(font,121),fill=accent)
 d.text((230,15),title,font=ImageFont.truetype(font,59),fill=(231,240,245))
 d.text((235,104),sub,font=ImageFont.truetype(font,40),fill=(197,214,228))
 im.save(p)
print('Three original typographic signs generated with project Noto font')
