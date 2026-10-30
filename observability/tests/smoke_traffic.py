"""Sends a little traffic to the running AppHost so dashboard panels have data to verify.

Usage: python3 -I observability/tests/smoke_traffic.py
Needs a running `ASPIRE_ENABLE_OBSERVABILITY_STACK=1 dotnet tool run aspire run` session.
"""
import json
import ssl
import subprocess
import urllib.request

PATHS = {"public-web-app": ["/", "/tours"], "web-app": ["/"], "catalog-api": ["/health"], "api-": ["/health"]}


def main():
    out = subprocess.run(["dotnet", "tool", "run", "aspire", "describe", "--format", "json"],
                         capture_output=True, text=True, check=True).stdout
    resources = json.loads(out[out.index("{"):])["resources"]
    context = ssl.create_default_context()
    context.check_hostname = False
    context.verify_mode = ssl.CERT_NONE  # local development certificates only
    for resource in resources:
        for prefix, paths in PATHS.items():
            if resource["name"].startswith(prefix):
                base = next((u["url"] for u in resource.get("urls", []) if u["url"].startswith("https://")), None)
                for _ in range(6):
                    for path in paths:
                        try:
                            urllib.request.urlopen(urllib.request.Request(base + path), context=context, timeout=15).close()
                        except Exception as error:  # redirects and 404 are fine; only reachability matters
                            getattr(error, "close", lambda: None)()
                print("sent", resource["name"], paths)


if __name__ == "__main__":
    main()
