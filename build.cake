#tool NUnit.ConsoleRunner


var target = Argument("target", "Test");
var configuration = Argument("configuration", "Release");
var platform = Argument("platform", "Any CPU");

var buildProject = File("DevExpressMods.sln");


Task("Clean")
    .Does(() =>
{
    MSBuild(buildProject, settings => settings.SetConfiguration(configuration).WithTarget("clean"));
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    MSBuild(buildProject, settings => settings.SetConfiguration(configuration).WithTarget("restore"));
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    MSBuild(buildProject, settings => settings.SetConfiguration(configuration));
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3(GetFiles("**/bin/" + configuration + "/**/*tests*.dll"), new NUnit3Settings { NoResults = true });
});

Task("Pack")
    .IsDependentOn("Test")
    .Does(() =>
{
    var version = System.Diagnostics.FileVersionInfo.GetVersionInfo("DevExpressMods/bin/Release/DevExpressMods.dll").ProductVersion;
    NuGetPack("DevExpressMods.nuspec", new NuGetPackSettings { Version = version });
});



RunTarget(target);
