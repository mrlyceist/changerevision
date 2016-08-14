ChangeRevision
===============

Semi-automatic version number incrementor for Visual Studio solutions based on numger of Git revisions.
It increases your Revision number (f.e. 2.3.5.X) if you build a debug assembly and increases your Build number by 1 every time you build a release assembly.

## Download ##

Compiled binary is located under bin/Release directory. Feel fre to download it.

## Instructions ##

1. Make sure, all four fields of *Assembly Version* and *File Version* in your **Assembly Information** are filled.
2. Modify your **AssemblyInfo.cs**:
- Delete commented string:
 `// [assembly: AssemblyVersion("1.0.*")]`
3. Place **ChangeRevision.exe** at the root of your solution directory.
4. Add a *Pre-Build Event*:
 `"$(SolutionDir)ChangeRevision.exe" $(ConfigurationName) "$(ProjectName)"`
 (including all of quotemarks!)
5. Build your project.

## Credits ##

This project is derivated from [Habrahabr post](https://habrahabr.ru/post/237585/). All credits at the code go to the autor of this post.

## Building ##

Just clone it and build it!