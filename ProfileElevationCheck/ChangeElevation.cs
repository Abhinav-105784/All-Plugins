using Autodesk.Civil.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using Autodesk.Civil.ApplicationServices;

[assembly: CommandClass(typeof(Profile_Elevation_check.ChangeElevation))]
namespace Profile_Elevation_check
{
    public class ChangeElevation
    {
        [CommandMethod("CHANGEELEVATIONS")]
        public void elevExtract()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            PromptEntityResult profile1Result = editor.GetEntity("Select the Source profile: ");
            if (profile1Result.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\nMain alignment not selected, try to re-run the command");
                return;
            }

            PromptEntityResult profile2Result = editor.GetEntity("Select the 2nd profile: ");
            if (profile2Result.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n2nd Alignment not selected, try to re-run the command");
                return;
            }
         /*   PromptDoubleOptions intervalInput = new PromptDoubleOptions("\nEnter the interval of stations")
            {
                AllowNegative = false,
                AllowNone = false,
                AllowZero = false,
            };
            PromptDoubleResult intervals = editor.GetDouble(intervalInput);
            if (intervals.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n Wrong Input");
                return;
            }*/
            PromptDoubleOptions alignment2StartPoint = new PromptDoubleOptions("\nAfter what length of main alignment 2nd alignment starts")
            {
                AllowNegative = false,
                AllowNone = false,
                AllowZero = true,
            };
            PromptDoubleResult alignment2StartPoint2 = editor.GetDouble(alignment2StartPoint);
            if (alignment2StartPoint2.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n Wrong Input");
                return;
            }
            using (Transaction trans = database.TransactionManager.StartTransaction())
            {
                CivilDocument cDoc = CivilApplication.ActiveDocument;
                Profile profile1st = trans.GetObject(profile1Result.ObjectId, OpenMode.ForRead) as Profile;
                Profile profile2nd = trans.GetObject(profile2Result.ObjectId, OpenMode.ForWrite) as Profile;
                editor.WriteMessage($"\n{profile2nd.PVIs.Count}");
                profile2nd.StyleId = profile1st.StyleId;
                ProfilePVI pv0 = profile2nd.PVIs.GetPVIAt(profile2nd.StartingStation, profile2nd.ElevationAt(profile2nd.StartingStation));
                double diffStart = profile1st.ElevationAt(alignment2StartPoint2.Value) - pv0.Elevation;
                editor.WriteMessage($"\ndifference: {diffStart:N3} source profile elevation: {profile1st.ElevationAt(alignment2StartPoint2.Value):N3} 2nd Profile elevation : {pv0.Elevation:N3}"); 
                pv0.Elevation += diffStart;
                editor.WriteMessage($"\n{pv0.Elevation:N3} at station {pv0.Station:N3}");
                ProfilePVI pv1 = profile2nd.PVIs.GetPVIAt(profile2nd.EndingStation, profile2nd.ElevationAt(profile2nd.Length));
                double diffEnd = profile1st.ElevationAt(alignment2StartPoint2.Value + profile2nd.Length) - pv1.Elevation;
                editor.WriteMessage($"\ndifference: {diffEnd:N3} source profile elevation: {profile1st.ElevationAt(alignment2StartPoint2.Value+profile2nd.Length):N3} 2nd Profile elevation : {pv1.Elevation:N3}");
                pv1.Elevation = pv1.Elevation + diffEnd;
                editor.WriteMessage($"\n{pv1.Elevation:N3} at station {pv1.Station:N3}");

                /*   for (double i = intervals.Value; i < profile2nd.Length; i += intervals.Value)
                   {

                       if (i >= profile2nd.StartingStation && i <= profile2nd.EndingStation)
                       {
                           double elevation2 = profile2nd.ElevationAt(i);
                           ProfilePVI profilePVI = profile2nd.PVIs.AddPVI(i, elevation2);
                           editor.WriteMessage($"\nOriginal elevation :{profilePVI.Elevation:N3}");
                           if (profile1st.ElevationAt((alignment2StartPoint2.Value) + (intervals.Value)) != profilePVI.Elevation)
                           {
                               profilePVI.Elevation = profile1st.ElevationAt((alignment2StartPoint2.Value) + (i));
                               editor.WriteMessage($"\nChanged Elevation :{profilePVI.Elevation:N3} source profile elevation :{profile1st.ElevationAt((alignment2StartPoint2.Value) + (i)):N3}\n");
                           }
                       }



                   }*/
                 foreach (ProfilePVI prof1PVI in profile1st.PVIs)
                 {
                     if (prof1PVI.Station > profile2nd.StartingStation && prof1PVI.Station < profile2nd.EndingStation)
                     {
                         double station1 = prof1PVI.Station;
                         double dist = station1 - alignment2StartPoint2.Value;
                         editor.WriteMessage($"\nElevation :{prof1PVI.Elevation:N3} on profile 1 {station1:N3} on 2nd profile {dist:N3}");
                         if (dist >= profile2nd.StartingStation && dist <= profile2nd.EndingStation)
                         {

                                 ProfilePVI prof2PVI = profile2nd.PVIs.AddPVI(profile2nd.StartingStation+dist, profile2nd.ElevationAt(profile2nd.StartingStation + dist));
                                 editor.WriteMessage($"\nOriginal elevation :{prof2PVI.Elevation:N3} at station {profile2nd.StartingStation + dist:N3} and original profile{station1:N3}");
                                 prof2PVI.Elevation = profile1st.ElevationAt(station1);
                                 editor.WriteMessage($"\nchanged elevation: {prof2PVI.Elevation:N3}");
                            editor.WriteMessage($"{profile2nd.PVIs.Count}");
                         }
                     }

                 }
                
                trans.Commit();
            }

        }
    }
}


