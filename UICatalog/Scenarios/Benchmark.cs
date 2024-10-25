using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Benchmark", "Benchmarks Terminal.Gui Layout and Draw Perf.")]
public sealed class Benchmark : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
        };

        ListView scenarioList = new ListView ()
        {
            Title = "_Sceanrios",
            BorderStyle = LineStyle.Rounded,
            Width = Dim.Auto (),
            Height = Dim.Fill (),
        };

        var types = AllScenarioTypes;
        ObservableCollection<string> scenarios = new ObservableCollection<string> (AllScenarioTypes.Select (
                                                                                                       t =>
                                                                                                       {
                                                                                                           var attr = t.GetCustomAttributes (
                                                                                                                    typeof (ScenarioMetadata),
                                                                                                                    false) [0] as ScenarioMetadata;

                                                                                                           return attr.Name;

                                                                                                       }));
        scenarioList.Source = new ListWrapper<string> (scenarios);

        scenarioList.Accepting += (sender, args) =>
                                  {

                                      bool waitForOutput = true;
                                      var output = string.Empty;



                                      //Task.Run (
                                      //  () =>
                                      //{
                                      using var process = new Process
                                      {
                                          StartInfo = new ()
                                          {
                                              FileName = "UICatalog.exe",
                                              Arguments = $"{scenarios [scenarioList.SelectedItem]}",
                                              RedirectStandardOutput = false,
                                              RedirectStandardError = false,
                                              RedirectStandardInput = false,
                                              UseShellExecute = true,
                                              CreateNoWindow = true
                                          }
                                      };

                                      process.Start ();

                                      if (!process.WaitForExit (10000))
                                      {
                                          var timeoutError =
                                              $@"Process timed out. Command line: {process.StartInfo.FileName} {process.StartInfo.Arguments}.";

                                          Debug.WriteLine (timeoutError);

                                          process.Close ();

                                          return;
                                      }
                                      //   });

                                  };

        appWindow.Add (scenarioList);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    public static IEnumerable<Type> AllScenarioTypes =>
        typeof (Scenario).Assembly
                         .GetTypes ()
                         .Where (type => type.IsClass && !type.IsAbstract && type.IsSubclassOf (typeof (Scenario)))
                         .Select (type => type);

}
