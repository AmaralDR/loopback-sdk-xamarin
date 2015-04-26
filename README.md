# README #

### What is this repository for? ###

Creation of Loopback SDK for Xamarin Studio or C# project.

* Quick Repository Review  

        The repository contains the SDK Generator: JS Code running a compiled c# code that creates the SDK. 
        To create an SDK, go into bin, then E.g. "node lb-xm d:\someserver\server\server.js" will create CS code for the SDK, 
        whereas "node lb-xm d:\someserver\server\server.js c sdk.dll" will compile an sdk DLL.
        In addition we have the C# part of the code, accessed by LBXamarinSDK.sln, and source files in C# directory. 
        This contains the open source of the c# part of the generator. This project's end result is 'LBXamarinSDKGenerator.dll' which
        is placed automatically as a post build event inside 'bin'. 
        The C# project contains also Testers for the lb-xm end result, for which we have the "test-server" folder, upon which the Testers act.
        Lastly, "SDK Example" folder contains the example server and Xamarin solution of an App using a compiled SDK.
        Last folder is "UnitTests", containing Testers for the project: it should get an input of the CS code generated by lb-xm.

* Version 1.0

### How do I get set up? ###

* On Windows 

        1. Run 'npm install' in shell.

* On MacOS 

        1. Have Homebrew installed - 'ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"'.
        2. If you don't have Mono x64 installed, run 'brew install https://raw.githubusercontent.com/tjanczuk/edge/master/tools/mono64.rb' in shell.
        3. run 'brew install pkg-config'.
        4. run 'npm install'.
	
* After Setup 

        To compile an SDK, make sure you have all the dependencies and run "node lb-xm d:\someserver\server\server.js c sdk.dll" in shell, 
        where first parameter is the server.js file of the Loopback server, 'c' is a compilation flag and sdk.dll is the filename of the output compiled SDK.
        To review the C# part of the Generator (Source code of lb-xm/bin/LBXamarinSDKGenerator.dll), take a look at the LBXamarinSDK.sln solution.
        To review the example App using a compiled SDK, take the folder "SDK Example". This folder in turn contains a Loopback server and a Xamarin solution of an Android App using the SDK.

### Testers for LBXamarinSDK ###

* Quick summary 

        The Unit Tests are located in the "C#\lb-xmTesters" folder.
        It contains a Xamarin/Visual studio testing solution on the test-server.

* Important notes 

        -The Testing unit is an example of building tests for a server
        -Calibrated to work with the test server, using classes which are in a LBXamarinSDK.cs file, created specifically for that server 
        -Changing the server without compiling a new LBXamarinSDK.cs will probably cause it not to work, or give false results


### How do I get set up? ###

** Setup**

* Install Server

        1. Go into test-server and run 'npm install' in terminal/shell

* Visual studio 

        1. It is important to do the setup while the project is open
        2. open 'lb-xmTesters.csproj' located in C#\lb-xmTesters
        3. Make sure you have NuGet installed:
		-Tools-> Extentions and Updates
		-Select online on left tab
		-Search for NuGet Package manager
        4. Make sure you have NUnit testing adapter installed:
		instructions: http://nunit.org/index.php?p=vsTestAdapter&r=2.6.3

* Xamarin 

        1. No special setup is needed

** Running Tests**

* All Platforms 

        1. Go into test-server and run 'slc run' (close the server if it had run, and run it again)
        !Important! This part should be done each time you run a test, as test change data in the server.
        2. If you made changes to the test-server
        -Comlpile a new LBXamatinSDK.cs and replace the existing one (node lb-xm SERVERPATH)
        -Make sure that the code corresponds to changes on expected test results

* Visual Studio 

        1. open 'lb-xmTesters.csproj' located in C#\lb-xmTesters
        2. Test -> Windows -> Test Explorer
        3. The first time you run tests click Run All in the tab that opens
        More info on NUnit can be foud here 
        http://nunit.org/index.php?p=vsTestAdapter&r=2.6.3

* Xamarin  

        1. open LBXamarinSDK.sln
        2. right click "lb-xmTesters" in the solution explorer -> run unit


### Contribution Workflow to the SDK Generator ###

* Write a failing test in lb-xmTest of the desired feature.
* Make changes to the SDK Generator C# project or/and test server or/and JS Code.
* Build the SDKGenerator C# project 
* Run the lb-xm to create a CS file and put it into the Testers project
* Run Test - Check feature functionality