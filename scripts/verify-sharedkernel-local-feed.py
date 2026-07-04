#!/usr/bin/env python3

from __future__ import annotations

import argparse
import shutil
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path
from zipfile import ZipFile


LOCAL_SOURCE_KEY = "sharedkernel-local"
NUGET_SOURCE_KEY = "nuget.org"
NUGET_SOURCE_URL = "https://api.nuget.org/v3/index.json"
RESTORE_CHECK_DIR = ".restore-check"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Verify SharedKernel packages restore from a local feed."
    )
    parser.add_argument("package_dir", type=Path)
    parser.add_argument("version")
    return parser.parse_args()


def read_nuspec(package_path: Path) -> ET.Element:
    with ZipFile(package_path) as package:
        nuspec_names = [name for name in package.namelist() if name.endswith(".nuspec")]
        if len(nuspec_names) != 1:
            raise ValueError(
                f"{package_path}: expected one .nuspec, found {len(nuspec_names)}"
            )

        return ET.fromstring(package.read(nuspec_names[0]))


def get_required_text(root: ET.Element, element_name: str, package_path: Path) -> str:
    element = find_first_by_local_name(root, element_name)
    if element is None or element.text is None or not element.text.strip():
        raise ValueError(f"{package_path}: missing {element_name}")

    return element.text.strip()


def local_name(tag: str) -> str:
    return tag.rsplit("}", maxsplit=1)[-1]


def find_first_by_local_name(root: ET.Element, element_name: str) -> ET.Element | None:
    return next(
        (element for element in root.iter() if local_name(element.tag) == element_name),
        None,
    )


def find_dependencies(root: ET.Element) -> list[ET.Element]:
    return [
        element for element in root.iter() if local_name(element.tag) == "dependency"
    ]


def read_package_ids(package_paths: list[Path], version: str) -> list[str]:
    package_ids: list[str] = []
    seen_package_ids: set[str] = set()
    duplicate_package_ids: set[str] = set()

    for package_path in package_paths:
        root = read_nuspec(package_path)
        package_id = get_required_text(root, "id", package_path)
        package_version = get_required_text(root, "version", package_path)
        if package_version != version:
            raise ValueError(
                f"{package_path}: expected version {version}, found {package_version}"
            )

        for dependency in find_dependencies(root):
            dependency_id = dependency.attrib.get("id", "")
            dependency_version = dependency.attrib.get("version", "")
            if (
                dependency_id.startswith("SharedKernel.")
                and dependency_version != version
            ):
                raise ValueError(
                    f"{package_path}: expected {dependency_id} "
                    f"dependency version {version}, "
                    f"found {dependency_version}"
                )

        if package_id in seen_package_ids:
            duplicate_package_ids.add(package_id)
        seen_package_ids.add(package_id)
        package_ids.append(package_id)

    if duplicate_package_ids:
        raise ValueError(
            f"duplicate packages: {', '.join(sorted(duplicate_package_ids))}"
        )

    return sorted(package_ids)


def write_restore_project(
    restore_dir: Path, package_ids: list[str], version: str
) -> Path:
    restore_dir.mkdir(parents=True, exist_ok=True)
    (restore_dir / "Directory.Build.props").write_text(
        "<Project />\n", encoding="utf-8"
    )
    (restore_dir / "Directory.Packages.props").write_text(
        "<Project />\n", encoding="utf-8"
    )
    references = "\n".join(
        f'    <PackageReference Include="{package_id}" Version="{version}" />'
        for package_id in package_ids
    )
    project_path = restore_dir / "SharedKernel.LocalFeedRestore.csproj"
    project_path.write_text(
        f"""<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
{references}
  </ItemGroup>
</Project>
""",
        encoding="utf-8",
    )

    return project_path


def add_package_source(parent: ET.Element, key: str, value: str) -> None:
    ET.SubElement(parent, "add", key=key, value=value)


def add_package_mapping(parent: ET.Element, key: str, patterns: list[str]) -> None:
    package_source = ET.SubElement(parent, "packageSource", key=key)
    for pattern in patterns:
        ET.SubElement(package_source, "package", pattern=pattern)


def write_nuget_config(
    restore_dir: Path,
    package_dir: Path,
    package_ids: list[str],
) -> Path:
    config_path = restore_dir / "NuGet.config"
    configuration = ET.Element("configuration")
    package_sources = ET.SubElement(configuration, "packageSources")
    ET.SubElement(package_sources, "clear")
    add_package_source(package_sources, LOCAL_SOURCE_KEY, str(package_dir.resolve()))
    nuget_source = ET.SubElement(
        package_sources, "add", key=NUGET_SOURCE_KEY, value=NUGET_SOURCE_URL
    )
    nuget_source.set("protocolVersion", "3")

    package_source_mapping = ET.SubElement(configuration, "packageSourceMapping")
    add_package_mapping(package_source_mapping, LOCAL_SOURCE_KEY, package_ids)
    add_package_mapping(package_source_mapping, NUGET_SOURCE_KEY, ["*"])

    ET.indent(configuration, space="  ")
    ET.ElementTree(configuration).write(
        config_path, encoding="utf-8", xml_declaration=True
    )

    return config_path


def restore_packages(
    project_path: Path, config_path: Path, package_cache_dir: Path
) -> None:
    subprocess.run(
        [
            "dotnet",
            "restore",
            str(project_path),
            "--configfile",
            str(config_path),
            "--packages",
            str(package_cache_dir),
            "--no-cache",
            "-p:RestorePackagesWithLockFile=false",
        ],
        check=True,
    )


def assert_restored(
    package_cache_dir: Path, package_ids: list[str], version: str
) -> None:
    missing = [
        package_id
        for package_id in package_ids
        if not (package_cache_dir / package_id.lower() / version.lower()).exists()
    ]
    if missing:
        raise ValueError(f"packages not restored from local feed: {', '.join(missing)}")


def main() -> int:
    args = parse_args()
    package_dir = args.package_dir
    if not package_dir.is_dir():
        raise ValueError(f"package directory not found: {package_dir}")

    package_paths = sorted(package_dir.glob("SharedKernel.*.nupkg"))
    package_paths = [
        path for path in package_paths if not path.name.endswith(".symbols.nupkg")
    ]
    if not package_paths:
        raise ValueError(f"no SharedKernel packages found in {package_dir}")

    package_ids = read_package_ids(package_paths, args.version)
    restore_dir = package_dir / RESTORE_CHECK_DIR
    if restore_dir.exists():
        shutil.rmtree(restore_dir)

    project_path = write_restore_project(restore_dir, package_ids, args.version)
    config_path = write_nuget_config(restore_dir, package_dir, package_ids)
    package_cache_dir = restore_dir / "packages"

    restore_packages(project_path, config_path, package_cache_dir)
    assert_restored(package_cache_dir, package_ids, args.version)

    print(f"local feed restore ok: {len(package_ids)} packages")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
