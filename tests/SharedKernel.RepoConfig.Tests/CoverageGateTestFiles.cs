using System.Xml.Linq;

namespace SharedKernel.RepoConfig.Tests;

internal static class CoverageGateTestFiles
{
    public static void Write(TemporaryRepoConfigWorkspace workspace, int coveredLines, string? conditions = null)
    {
        workspace.WriteFile("src/Core.cs", "one\ntwo\nthree\nfour\nfive\n");
        workspace.WriteFile("changes.patch", "--- /dev/null\n+++ b/src/Core.cs\n@@ -0,0 +1,5 @@\n+one\n+two\n+three\n+four\n+five\n");
        var lines = Enumerable.Range(1, 5).Select(number =>
        {
            var line = new XElement("line", new XAttribute("number", number), new XAttribute("hits", number <= coveredLines ? 1 : 0));
            if (number == 1 && conditions is not null)
            {
                line.Add(new XAttribute("branch", "true"), new XAttribute("condition-coverage", conditions));
            }
            return line;
        });
        var document = new XDocument(new XElement("coverage", new XElement("packages", new XElement("package",
            new XElement("classes", new XElement("class", new XAttribute("filename", "src/Core.cs"), new XElement("lines", lines)))))));
        workspace.WriteFile("coverage.xml", document.ToString());
    }

    public static void ApplyScenario(TemporaryRepoConfigWorkspace workspace, string scenario)
    {
        switch (scenario)
        {
            case "duplicate":
                var report = XDocument.Parse(workspace.ReadFile("coverage.xml"));
                var classes = report.Descendants("classes").Single();
                var duplicate = new XElement(classes.Elements("class").Single());
                foreach (var line in duplicate.Descendants("line").Where(line => (int?)line.Attribute("number") > 3).ToArray())
                {
                    line.Remove();
                }
                classes.Add(duplicate, new XElement(duplicate), new XElement(duplicate));
                workspace.WriteFile("coverage.xml", report.ToString());
                break;
            case "rename":
                workspace.WriteFile("changes.patch", "diff --git a/src/Old.cs b/src/Core.cs\nsimilarity index 90%\nrename from src/Old.cs\nrename to src/Core.cs\n--- a/src/Old.cs\n+++ b/src/Core.cs\n@@ -1 +1 @@\n-old\n+new\n");
                break;
            case "delete":
                workspace.WriteFile("changes.patch", "--- a/src/Core.cs\n+++ /dev/null\n@@ -1,5 +0,0 @@\n-one\n-two\n-three\n-four\n-five\n");
                break;
            case "non-executable":
                workspace.WriteFile("changes.patch", "--- a/src/Core.cs\n+++ b/src/Core.cs\n@@ -6 +6 @@\n-// old\n+// new\n");
                break;
            case "tests-only":
                workspace.WriteFile("changes.patch", "--- /dev/null\n+++ b/tests/CoreTests.cs\n@@ -0,0 +1 @@\n+test\n");
                break;
            case "malformed-hunk":
                workspace.WriteFile("changes.patch", "--- /dev/null\n+++ b/src/Core.cs\n@@ bad @@\n+one\n");
                break;
            case "truncated-hunk":
                workspace.WriteFile("changes.patch", "--- /dev/null\n+++ b/src/Core.cs\n@@ -0,0 +1,5 @@\n+one\n");
                break;
            case "invalid-start":
                workspace.WriteFile("changes.patch", "--- /dev/null\n+++ b/src/Core.cs\n@@ -0,0 +0,1 @@\n+one\n");
                break;
            case "header-without-hunk":
                workspace.WriteFile("changes.patch", "--- /dev/null\n+++ b/src/Core.cs\n");
                break;
            case "missing-diff":
                workspace.DeleteFile("changes.patch");
                break;
            case "reportgenerator-doctype":
                Write(workspace, 5);
                workspace.WriteFile("coverage.xml", "<!DOCTYPE coverage SYSTEM 'http://cobertura.sourceforge.net/xml/coverage-04.dtd'>\n" + workspace.ReadFile("coverage.xml"));
                break;
            case "internal-entity":
                workspace.WriteFile("coverage.xml", "<!DOCTYPE coverage [<!ENTITY value 'hidden'>]><coverage>&value;</coverage>");
                break;
            case "dtd":
                workspace.WriteFile("coverage.xml", "<!DOCTYPE coverage [<!ENTITY secret SYSTEM 'file:///etc/passwd'>]><coverage>&secret;</coverage>");
                break;
            default:
                throw new ArgumentException("Unknown coverage scenario.", nameof(scenario));
        }
    }
}
