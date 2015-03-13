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

## Build Environment
To assist with automating build processes, a number of environment variables are set by Marathon to indicate
properties of the build taking place. You'd generally use them within build/deployment scripts to specify
output directories or file transforms - for example, setting your assembly version.

- **CI_SERVER** is set to `yes` to indicate that your build is running on a CI server.
- **CI\_SERVER\_NAME** is set to `GitLab CI` to indicate the type of CI server running your build.
- **CI\_SERVER\_VERSION** is currently not specified.
- **CI\_SERVER\_REVISION** is currently not specified.
- **CI\_BUILD\_REF** is set to the full SHA hash of the commit for which you are conducting a build.
- **CI\_BUILD\_REF\_NAME** is set to the branch or tag name for which the build is being conducted.
- **CI\_BUILD\_REF\_PREVIOUS** is set to the full SHA hash of the commit preceding this build.
- **CI\_BUILD\_ID** is set to the unique build ID used internally by GitLab CI to identify the build.

In addition to this, the working directory of your build script is set to the build path - which matches
the root of your repository.

## Configuration
The `config.json` file used by Marathon can be easily edited if you wish to change any of the configuration
options later. In addition to this, you can specify the shell you want to use (**cmd** by default) and the
directory in which builds are conducted.

```json
{
	"url": "https://ci.gitlab.org",
	"token": "aaaaaaaaaaaaaaaaaaaaaaaa",
	"builds_path": "C:\\Builds",
	"shell": "cmd",
	"fail_fast": false,
	"environment": {
		"deploy_path": "C:\\Deployment"
	},
	"setup": {
		"retrieve npm packages": "npm install"
	}
}
```

### Configuration Options
There are a number of configuration options available to you, all except **url** and **token** are optional
and Marathon will attempt to guess logical values for them if you don't specify them.

 - **url** is the URL of your GitLab CI coordinator from which builds will be retrieved.
 - **token** is the token received from the GitLab CI coordinator in response to a registration request.
   This isn't the same as the token you use to register the runner in the first place.
 - **builds_path** allows you to specify a custom directory in which builds will be conducted.
   By default Marathon will use `tmp/builds`
 - **shell** allows you to specify the shell you want to use when running your build scripts.
   Marathon defaults to `cmd` but you can also use `powershell`.
 - **fail_fast** can be used to include exit status checks in the generated shell script after
   each command - allowing you to exit the build process immediately if any error is encountered.
 - **environment** can be used to specify environment variables which will be available within
   your build script. It's useful if you want each runner to behave slightly differently.
 - **setup** lets you specify commands to be run before your build script. It's generally used
   to configure your build environment. \*

 \* Due to the way our configuration system works, we cannot use Arrays for values. Specify any
 name you like for the **setup** entry ID - only the value (the command to be run) matters.

## Installing as A Service
Chances are you don't want to manually start Marathon every time you boot up your computer - the best
way to avoid this is by installing it as a Windows Service. We recommend using [NSSM](http://nssm.cc/)
to do so as it works exceptionally well out of the box.