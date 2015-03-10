# Marathon
**A GitLab CI Runner for Windows**

Marathon was developed by [EMSS Consulting](http://www.emss.co.za) to power our next generation build
process - which is heavily reliant on .NET and Windows. Since the existing GitLab CI build runners for
Windows were lacking features we decided to develop our own.

## Installation
To perform first time setup of the GitLab CI Runner you should run `marathon setup`. This will walk you
through the process of configuring Marathon to connect to your GitLab CI instance.

## Running Marathon
Marathon can be started by using `marathon start` from the command line - this will begin polling your
configured GitLab CI server for builds and executing any that are returned to the current Marathon instance.

## Configuration
The `config.json` file used by Marathon can be easily edited if you wish to change any of the configuration
options later. In addition to this, you can specify the shell you want to use (**cmd** by default) and the
directory in which builds are conducted.

```json
{
	"url": "https://ci.gitlab.org",
	"token": "aaaaaaaaaaaaaaaaaaaaaaaa",
	"builds_path": "C:\\Builds",
	"shell": "cmd"
}
```

## Installing as A Service
Chances are you don't want to manually start Marathon every time you boot up your computer - the best
way to avoid this is by installing it as a Windows Service. We recommend using [NSSM](http://nssm.cc/)
to do so as it works exceptionally well out of the box.