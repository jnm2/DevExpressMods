# Walkthrough: Extending the XCRM Sample

This sample uses DevExpress's XCRM sample code as a starting point. It is found at `%public%\Documents\DevExpress Demos 16.1\Components\eXpressApp Framework\XCRM\CS`.

> **If the path does not exist:**
> 
> Run the DevExpress installer and make sure the eXpressApp Framework is installed. The easiest way to do that is Control Panel > Programs and Features > DevExpress Components 16.1 > Uninstall/Change > Modify.

Before we start modifying the sample we will make a copy of the entire folder so that the original remains untouched. Copy the `CS` folder to the desktop or wherever your preferred scratch space is, and rename it `XAF SummaryField sample` or similar.

## Step 0: Set up

Open `XCRM.sln` inside your scratch copy of the XCRM sample. Make sure the `XCRM.Win` project is selected as the default startup project. The web projects can be ignored.

**This sample requires a connection to a SQL Server instance in order to run with permissions to create a database.**

If you don't have a test instance of SQL Server, download the latest version of [SQL Server Developer Edition](https://myprodscussu1.app.vssubscriptions.visualstudio.com/Downloads?q=SQL%20Server%20Developer) for free and install it. Step by step instructions are beyond the scope of this walkthrough. For the purposes of test instances in general and this sample in specific, I recommend leaving the default setting whereby your user account becomes a database admin.

Once you have a test instance and database admin credentials ready, open`XCRM.Win\App.config` and edit this line:

```xml
<add name="SqlExpressConnectionString" connectionString="Integrated Security=SSPI;MultipleActiveResultSets=True;Data Source=.\SQLEXPRESS;Initial Catalog=XCRM_16.1" />
```

Change the name of the connection string from `SqlExpressConnectionString` to `ConnectionString`, and change the connection string to match the instance you will be using. Ideally you will only need to change the Data Source.

Finally, run `XCRM.Win` and make sure it gets to the sample Log On window so you know everything has been set up properly. It takes a while before anything shows up, so be patient.

## Step 1: Add DevExpressMods reference

Install the [DevExpressMods NuGet package](https://www.nuget.org/packages/DevExpressMods/) into the `XCRM.Module` project:

 * Right click the `XCRM.Module` project in Solution Explorer and select `Manage NuGet Packages...`
 * Click the Browse tab. In the search bar, type `DevExpressMods` and press enter.
 * Select the DevExpressMods package and install the latest release version, currently [![NuGet](http://img.shields.io/nuget/v/DevExpressMods.svg?maxAge=2592000)](https://www.nuget.org/packages/DevExpressMods/).

## Step 2: Add DevExpress.ExpressApp.ReportsV2.Win reference

> If you have ReSharper, you can skip this step and leverage the suggested reference for the `WinReportServiceController` type in the source code you add in the next step.

 * Right click the References node under the `XCRM.Module` project in Solution Explorer and select `Add Reference...`
 * Click the Assemblies group. In the search bar, paste `DevExpress.ExpressApp.ReportsV2.Win` and do not press enter.
 * Check the box next to `DevExpress.ExpressApp.ReportsV2.Win` and press enter.

## Step 3: 

Add a file to the `XCRM.Module` project named `ReportsServiceController.cs` with the following code:

```c#
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ReportsV2.Win;
using DevExpressMods.Features;

namespace XCRM.Module
{
    public class ReportsServiceController : Controller
    {
        private WinReportServiceController winReportsServiceController;

        protected override void OnActivated()
        {
            base.OnActivated();
            winReportsServiceController = Frame.GetController<WinReportServiceController>();
            winReportsServiceController.DesignFormCreated += winReportsServiceController_DesignFormCreated;
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            winReportsServiceController.DesignFormCreated -= winReportsServiceController_DesignFormCreated;
        }

        private static void winReportsServiceController_DesignFormCreated(object sender, DesignFormEventArgs e)
        {
            SummaryFieldsFeature.Apply(e.DesignForm);
        }
    }
}
```

For your convenience [ReportsServiceController.cs](ReportsServiceController.cs) is located beside this document. [Courtesy](https://gitter.im/jnm2/DevExpressMods?at=578836403c5129720e263ee4) of ScottGross.


## Finished

And that's all there is to it! Run the sample and click Log On. In the main window, click the ellipsis in the bottom left corner and click Reports. In the sidebar, click Reports. Then, open the report designer either by:

 1. Right clicking in the grid and clicking New and using the new report wizard
 2. Selecting an existing report, clicking Copy Predefined Report in the ribbon, selecting the copy that is created, and clicking Show Report Designer in the ribbon.

In the report designer window, right click on a data member or field in the Field List and see the new Add Summary Field menu item. For instructions on how to use summary fields, see the main readme.
