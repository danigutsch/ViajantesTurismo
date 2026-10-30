"""Static contract for source-controlled observability assets.

Run: python3 -I -m unittest discover -s observability/tests -v
"""
import json
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
OBS = ROOT / "observability"
DASHBOARDS = OBS / "grafana" / "dashboards"
ALERTING = OBS / "grafana" / "provisioning" / "alerting"
CATALOG = ROOT / "docs" / "OBSERVABILITY_CATALOG.md"
RUNBOOKS = ROOT / "docs" / "OBSERVABILITY_RUNBOOKS.md"

EXPECTED_DASHBOARDS = {
    "platform-smoke",
    "postgresql-observability",
    "collector-gateway",
    "http-platform",
    "catalog-pipeline",
    "media-accessibility",
    "branding-cache",
    "event-store",
    "logs",
    "traces",
    "runtime",
}

REGISTERED_METERS = {
    "SharedKernel.Mediator",
    "ViajantesTurismo.Catalog",
    "ViajantesTurismo.Branding",
    "PandaBear.AspNetCore",
    "PandaBear.Blazor",
    "SharedKernel.EventSourcing.Npgsql",
    "SharedKernel.Observability.Npgsql",
}


def load_dashboards():
    return {p.stem: json.loads(p.read_text()) for p in sorted(DASHBOARDS.glob("*.json"))}


class DashboardContract(unittest.TestCase):
    def test_every_surface_has_a_dashboard(self):
        self.assertEqual(EXPECTED_DASHBOARDS, set(load_dashboards()))

    def test_uid_matches_file_name(self):
        for name, d in load_dashboards().items():
            self.assertEqual(name, d["uid"], name)

    def test_first_panel_states_purpose(self):
        for name, d in load_dashboards().items():
            first = d["panels"][0]
            self.assertEqual("text", first["type"], name)
            content = first["options"]["content"]
            for heading in ("Purpose", "Audience", "Decision"):
                self.assertIn(heading, content, f"{name}: missing {heading}")

    def test_every_data_panel_has_description_and_unit(self):
        for name, d in load_dashboards().items():
            for p in d["panels"][1:]:
                if p["type"] in ("row", "text"):
                    continue
                self.assertTrue(p.get("description", "").strip(), f"{name}/{p['title']}: description")
                if p["type"] != "logs" and p["type"] != "table":
                    self.assertIn("unit", p["fieldConfig"]["defaults"], f"{name}/{p['title']}: unit")

    def test_every_data_panel_has_a_query(self):
        for name, d in load_dashboards().items():
            for p in d["panels"][1:]:
                if p["type"] in ("row", "text"):
                    continue
                self.assertTrue(p["targets"], f"{name}/{p['title']}: targets")

    def test_no_forbidden_grouping_dimensions(self):
        forbidden = re.compile(r"by \([^)]*(trace_id|span_id|stream_id|event_id|exception|user|email)", re.I)
        for name, d in load_dashboards().items():
            for p in d["panels"]:
                for t in p.get("targets", []):
                    self.assertIsNone(forbidden.search(t.get("expr", "")), f"{name}/{p['title']}")

    def test_panel_ids_unique(self):
        for name, d in load_dashboards().items():
            ids = [p["id"] for p in d["panels"]]
            self.assertEqual(len(ids), len(set(ids)), name)


class AlertContract(unittest.TestCase):
    def rules(self):
        text = (ALERTING / "rules.yaml").read_text()
        blocks = re.split(r"\n\s+- uid: ", text)[1:]
        return [b for b in blocks]

    def test_alert_provisioning_files_exist(self):
        for f in ("rules.yaml", "contact-points.yaml", "policies.yaml", "templates.yaml"):
            self.assertTrue((ALERTING / f).is_file(), f)

    def test_every_rule_has_summary_description_runbook(self):
        rules = self.rules()
        self.assertGreaterEqual(len(rules), 5)
        for r in rules:
            for key in ("summary:", "description:", "runbook:"):
                self.assertIn(key, r, r.splitlines()[0])

    def test_every_runbook_anchor_exists(self):
        runbooks = RUNBOOKS.read_text()
        anchors = set(re.findall(r"runbook: \S*#(\S+)", (ALERTING / "rules.yaml").read_text()))
        self.assertTrue(anchors)
        for a in anchors:
            self.assertIn(f"## {a.replace('-', ' ')}", runbooks.lower().replace("\n## ", "\n## "), a)

    def test_rules_do_not_group_by_identifiers(self):
        text = (ALERTING / "rules.yaml").read_text()
        self.assertIsNone(re.search(r"by \([^)]*(trace_id|span_id|stream_id|event_id|exception)", text))


class CatalogContract(unittest.TestCase):
    def test_catalog_lists_every_registered_meter(self):
        text = CATALOG.read_text()
        for meter in REGISTERED_METERS:
            self.assertIn(f"`{meter}`", text, meter)

    def test_registered_meters_in_source_are_known(self):
        src = (ROOT / "src/ViajantesTurismo.ServiceDefaults/OpenTelemetryBuilderExtensions.cs").read_text()
        found = set(re.findall(r'const string \w+Name = "([^"]+)"', src))
        self.assertTrue(found <= REGISTERED_METERS, found - REGISTERED_METERS)

    def test_every_row_has_verdict(self):
        rows = [l for l in CATALOG.read_text().split("## Metrics", 1)[1].splitlines() if l.startswith("| `")]
        self.assertGreater(len(rows), 20)
        for r in rows:
            self.assertRegex(r, r"\| (keep|remove|gap) \|?\s*$", r)

    def test_every_dashboard_metric_is_catalogued(self):
        text = CATALOG.read_text()
        for name, d in load_dashboards().items():
            for p in d["panels"]:
                for t in p.get("targets", []):
                    for m in re.findall(r"\b([a-z][a-z0-9_]+_(?:total|bucket|seconds|milliseconds))\b", t.get("expr", "")):
                        base = re.sub(r"_(milliseconds|seconds)$", "", re.sub(r"_(bucket|count|sum)$", "", m))
                        if base.startswith(("otelcol_", "http_", "kestrel_", "dotnet_", "process_", "aspnetcore_", "db_client", "client_call", "otelcol_", "up")):
                            continue
                        self.assertIn(base.removesuffix("_total"), text.replace(".", "_"), f"{name}: {m}")


if __name__ == "__main__":
    unittest.main()
