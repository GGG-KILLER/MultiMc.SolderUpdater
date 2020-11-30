## v1.4.2
Changes:

- Change lockfile file handling to truncate it if it exists and create it if it doesn't.


## v1.4.1
Changes:

- Change lockfile file handling delete it before writing.

## v1.4.0
Changes:

- Change sorting of zip paths when deleting older versions of mods so that nested directories and files are deleted before its ancestors;
- Change saving of lockfile to truncate file when writing;
- Change directory check used when deleting files;
- Change working directory to instance path when doing operations;
- Implement deletion of all mods because of the bug mentioned on the first and second items.

## v1.3.1
Changes:

- Don't skip forge mod.

## v1.3.0
Changes:

- Update to .NET 5;
- Migrate to System.Text.Json;
- Parallelize mod downloading;
- Fix forge skipping;
- Send updater version in User-Agent;
- Reformat codebase.

## v1.2.1
Changelog:

- Fix the updater deleting all mods and not redownloading them

## v1.2.0
Changelog:

- Fixes update logic to delete removed mods before updating the rest of the mods