"""Live check: every dashboard panel query returns data from the running local stack.

Usage: python3 -I observability/tests/verify_live.py --grafana http://127.0.0.1:3000

Panels carry a "verify" key: "data" (must return series after smoke traffic) or "idle-ok"
(may be empty on a healthy system, such as error counters). Exit code 1 on any failure.
"""
import argparse
import base64
import json
import pathlib
import sys
import time
import urllib.parse
import urllib.request

DASHBOARDS = pathlib.Path(__file__).resolve().parents[1] / "grafana" / "dashboards"


def get(url, auth):
    req = urllib.request.Request(url, headers={"Authorization": auth} if auth else {})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.load(r)


def run_target(grafana, auth, ds_type, expr):
    now = int(time.time())
    if ds_type == "prometheus":
        q = urllib.parse.urlencode({"query": expr.replace("$service", ".*").replace("$__rate_interval", "5m").replace("$__interval", "1m")})
        r = get(f"{grafana}/api/datasources/proxy/uid/Prometheus/api/v1/query?{q}", auth)
        return r["status"] == "success", len(r["data"]["result"])
    if ds_type == "loki":
        q = urllib.parse.urlencode({"query": expr.replace("$__auto", "5m").replace("$__range", "1h"),
                                    "start": (now - 3600) * 10**9, "end": now * 10**9, "limit": 1})
        r = get(f"{grafana}/api/datasources/proxy/uid/Loki/loki/api/v1/query_range?{q}", auth)
        return r["status"] == "success", len(r["data"]["result"])
    if ds_type == "tempo":
        q = urllib.parse.urlencode({"q": expr, "start": now - 3600, "end": now, "limit": 1})
        r = get(f"{grafana}/api/datasources/proxy/uid/Tempo/api/search?{q}", auth)
        return True, len(r.get("traces", []))
    raise ValueError(ds_type)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--grafana", required=True)
    ap.add_argument("--user", default="")
    ap.add_argument("--password", default="")
    a = ap.parse_args()
    auth = "Basic " + base64.b64encode(f"{a.user}:{a.password}".encode()).decode() if a.user else ""
    failures = []
    for f in sorted(DASHBOARDS.glob("*.json")):
        d = json.loads(f.read_text())
        for p in d["panels"]:
            if p["type"] in ("row", "text"):
                continue
            mode = p.get("verify", "data")
            for t in p.get("targets", []):
                ds = t.get("datasource", p.get("datasource", {})).get("type")
                ok, n = run_target(a.grafana, auth, ds, t["expr"])
                status = "ok" if ok and (n > 0 or mode == "idle-ok") else "FAIL"
                print(f"{status:4} {d['uid']}/{p['title']} [{t['refId']}] series={n} mode={mode}")
                if status == "FAIL":
                    failures.append(f"{d['uid']}/{p['title']}")
    if failures:
        print(f"\n{len(failures)} failing panel(s)")
        sys.exit(1)


if __name__ == "__main__":
    main()
