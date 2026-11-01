"""Small local MCP diagnostic client; no external Python dependencies."""
import json
import sys
import urllib.request


class Client:
    def __init__(self):
        self.headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        self.sequence = 0
        result = self.request("initialize", {"protocolVersion": "2024-11-05", "capabilities": {},
                                             "clientInfo": {"name": "EchoStation-development", "version": "1.0"}})
        self.headers["MCP-Protocol-Version"] = result["protocolVersion"]
        self.request("notifications/initialized", notification=True)

    def request(self, method, params=None, notification=False):
        self.sequence += 1
        payload = {"jsonrpc": "2.0", "method": method, "params": params or {}}
        if not notification:
            payload["id"] = self.sequence
        req = urllib.request.Request("http://localhost:8080/mcp", json.dumps(payload).encode(), self.headers)
        with urllib.request.urlopen(req, timeout=50) as response:
            session = response.headers.get("Mcp-Session-Id")
            if session:
                self.headers["Mcp-Session-Id"] = session
            body = response.read().decode()
        if not body:
            return None
        if any(line.startswith("data:") for line in body.splitlines()):
            messages = [json.loads(line[5:].strip()) for line in body.splitlines() if line.startswith("data:")]
            result = next(item for item in messages if item.get("id") == payload.get("id"))
        else:
            result = json.loads(body)
        if "error" in result:
            raise RuntimeError(result["error"])
        return result.get("result")


if __name__ == "__main__":
    client = Client()
    if sys.argv[1] == "resource":
        result = client.request("resources/read", {"uri": sys.argv[2]})
    elif sys.argv[1] == "list":
        result = client.request("tools/list")
    else:
        result = client.request("tools/call", {"name": sys.argv[1], "arguments": json.loads(sys.argv[2])})
    print(json.dumps(result, ensure_ascii=True, indent=2))
