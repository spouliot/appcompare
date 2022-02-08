Compare two application bundles.

In it's most basic form, the tool will compare the contents of two 
directories (it does not have to be application bundles). This is
similar to my original [app-compare](https://github.com/spouliot/dotnet-tools/tree/master/app-compare)
tool.

The goal of this complete rewrite is to build/integrate more tools that
will help you to understand the differences between the two bundles.
E.g. why has my app grown so much ?

## How to install

Release packages are available from nuget. They can be installed from the
command-line.

```bash
$ dotnet tool install --global appcompare
```

To update or to re-install the latest version execute:

```bash
$ dotnet tool update --global appcompare
```

## How to use

You can use the tool from the command line and use it to automate (CI/CD)
the process of comparing your current application bundle with the previous
(e.g. release) one.

A text-user interface is also available to allow you to dig further into
the differences between the two bundles.

### With the Command Line

```bash
$ appcompare path/to/bundle/1 path/to/bundle/2 -output-markdown report.md
```

You can also use the `--gist` option to generate a [gist](https://github.com/spouliot/appcompare/wiki/Gist)
from the output. The url of the gist will be printed to stdout.

```bash
$ appcompare path/to/bundle/1 path/to/bundle/2 --gist
```

### With the Text User Interface

* Press `F1` to select the first application bundle.
* Press `F2` to select the second application bundle.
* Press `CTRL+G` to create a gist of the comparison.

Voila! Now you can play around to find out what's different between
the two bundles.

## When to use

Ideally you keep copies of every released version of your application. 
Every time you release a new version, you should compare the previous
version with the new one. This way you can see what and where are the 
changes inside your application and how this affected the build size.

## Dependencies

Built on top
* [CliWrap](https://github.com/Tyrrrz/CliWrap)
* [Spectre.Console](https://spectreconsole.net)
* [Terminal.Gui](https://github.com/migueldeicaza/gui.cs)

Released with
* [dotnet-releaser](https://github.com/xoofx/dotnet-releaser)
