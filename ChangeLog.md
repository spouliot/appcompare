# ChangeLog

## 2.1.0 (13 Feb 2022)

### Features
- Add a custom tool analyzer (`SHIFT+T`) to compare two files, e.g. run `otool -L` on both files and show the `diff -u` between them.
- Add selected file (`SHIFT+J`) or all files (`SHIFT+I`) identification
- Add selected file comparison (`SHIFT+C`)

## 2.0.1 (8 Feb 2022)

### Fixes
- Fix `ArgumentNullException` if no directories are provided from the command-line
- Show the [Gist](https://github.com/spouliot/appcompare/wiki/Gist) documentation if an error occurs while gisting
- Correctly compare directories ending (or not) with a slash
- Fix nuget package information (license, readme, changelog...)

## 2.0.0 (7 Feb 2022)

### Features
- Compare file sizes inside app bundle or directories
- Export report to markdown (files or gists)
- Use from the command-line or invoke the text user interface
