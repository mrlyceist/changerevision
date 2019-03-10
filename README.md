# ChangeRevision

Semi-automatic version number incrementor for Visual Studio solutions based on number of Git revisions.
It increases your Build number (f.e. 2.3.X) if you build a debug assembly and increases your Minor number by 1 every time you build a release assembly.
Compatible with **.Net Framework**, **.Net Standard** and **.Net Core** projects. [SemVer](http://semver.org) compatible.

## Download

Compiled binary is located under bin/Release directory. Feel fre to download it.

## Using

this little executable can work with oldschool **framework** projects and with new **core** projects. It can determine type of used project by itself.

*Bonus!* It can even update your NuGet package version!

### For .NetFramework projects

1. Make sure, all four fields of *Assembly Version* and *File Version* in your **Assembly Information** are filled.
2. Modify your **AssemblyInfo.cs**:
    1. Delete commented string: `// [assembly: AssemblyVersion("1.0.*")]`
3. Place **ChangeRevision.exe** at the root of your solution directory.
4. Add a *Post-Build Event*:
 `"$(SolutionDir)ChangeRevision.exe" $(ConfigurationName) "$(ProjectName)"`
 (including all of quotemarks!)
5. Build your project.

### For .NetCore / .NetStandard projects

1. In your project properties, on *package* tab, make sure fields **Assembly version** and **Assembly file version** are filled.
2. If you're generating NuGet package, make sure that the **version** field is also filled.

If you're generating NuGet package using ***.nuspec** file, your project settings are irrelevant. This executable will search for a field **version** in your ***.nuspec** file and update it's value.

## Notice

This executable is meant to be a *Post-Build event*, so it will update suitable properties *after* you build your solution. So you might be wanting to rebuild your project after version increment.

## Credits

This project is inspired by [Habrahabr post](https://habrahabr.ru/post/237585/). All credits at the code go to the autor of this post.

## Building

Just clone it and build it!