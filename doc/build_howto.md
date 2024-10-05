# How to build without Visual Studio

1. Install
   [Microsoft Build Tools 2015](https://www.microsoft.com/en-us/download/details.aspx?id=48159).
   Take note of the installation path. Default is:
   `C:\Program Files (x86)\MSBuild\14.0\Bin`

2. Edit your `PATH` environment variable to include that installation path.

3. Define the environment variable `SIMHUB_INSTALL_PATH`, pointing to
   `C:\Program Files (x86)\SimHub\` (or your SimHub installation folder).
   **Note the trailing backslash**. It will not work without it.

4. Open a terminal and change the current directory to that where de `.sln` file is found.
   Type `msbuild` to build and install the plugin in debug configuration.

5. For release, type `msbuild /P:configuration=release`

## Visual Studio Code

Visual Studio Code features some extension to work with C# and *.NET*.
However, it does not work with "non-SDK-style" projects as this one.
It works just as a code editor.
