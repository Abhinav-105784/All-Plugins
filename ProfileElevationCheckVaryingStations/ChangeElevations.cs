using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
[assembly:CommandClass(typeof(Profile_Elevation_check_Varying_distance.ChangeElevations))]
namespace Profile_Elevation_check_Varying_distance
{
    public class ChangeElevations
    {
        [CommandMethod("CHANGEELEVATIONSVARYINGSTATIONS")]
        public void ElevChange()
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
            PromptDoubleOptions distanceInput1 = new PromptDoubleOptions($"\nEnter the Starting point of the 2nd alignment from 1st : ")
            {
                AllowNegative = false,
                AllowNone = false,
                AllowZero = true,
            };
            PromptDoubleResult distanceResult2 = editor.GetDouble(distanceInput1);
            if (distanceResult2.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n Wrong Input");
                return;
            }
            using (Transaction trans = database.TransactionManager.StartTransaction())
            {
                CivilDocument cDoc = CivilApplication.ActiveDocument;
                Profile profile1st = trans.GetObject(profile1Result.ObjectId, OpenMode.ForRead) as Profile;
                Profile profile2nd = trans.GetObject(profile2Result.ObjectId, OpenMode.ForWrite) as Profile;
                //profile2nd.StyleId = profile1st.StyleId;
                ProfilePVI pv0 = profile2nd.PVIs.GetPVIAt(profile2nd.StartingStation, profile2nd.ElevationAt(profile2nd.StartingStation));
                pv0.Elevation = profile1st.ElevationAt(distanceResult2.Value);
                editor.WriteMessage($"\n{pv0.Elevation:N3} at station {pv0.Station:N3}");
                ProfilePVI pv1 = profile2nd.PVIs.GetPVIAt(profile2nd.EndingStation, profile2nd.ElevationAt(profile2nd.Length));
                pv1.Elevation = profile1st.ElevationAt(distanceResult2.Value + profile2nd.Length);
                editor.WriteMessage($"\n{pv1.Elevation:N3} at station {pv1.Station:N3}");
                foreach (ProfilePVI prof1PVI in profile1st.PVIs)
                {
                    
                    double station1 = prof1PVI.Station;
                    

                    if (prof1PVI.Station>=distanceResult2.Value && prof1PVI.Station <= profile2nd.EndingStation)
                    {
                        PromptDoubleOptions distanceInput = new PromptDoubleOptions($"\nEnter the distance on the 2nd alignment for PVI at station on 1st profile {prof1PVI.Station:N3}: ")
                        {
                            AllowNegative = false,
                            AllowNone = false,
                            AllowZero = true,
                        };
                        PromptDoubleResult distanceResult = editor.GetDouble(distanceInput);
                        if (distanceResult.Status != PromptStatus.OK)
                        {
                            editor.WriteMessage("\n Wrong Input");
                            return;
                        }
                        double distanceOnSecondAlignment = distanceResult.Value;
                       // double dist = distanceOnSecondAlignment;
                        editor.WriteMessage($"\nElevation :{prof1PVI.Elevation:N3} on profile 1 {station1:N3} on 2nd profile {distanceOnSecondAlignment:N3}");

                        if (distanceOnSecondAlignment >= profile2nd.StartingStation && distanceOnSecondAlignment <= profile2nd.EndingStation)
                        {
                            ProfilePVI prof2PVI = profile2nd.PVIs.AddPVI(distanceOnSecondAlignment, profile2nd.ElevationAt(distanceOnSecondAlignment));
                            editor.WriteMessage($"\nOriginal elevation :{prof2PVI.Elevation:N3} at station {distanceOnSecondAlignment:N3} and original profile {station1:N3}");
                            prof2PVI.Elevation = profile1st.ElevationAt(station1);
                            editor.WriteMessage($"\nChanged elevation: {prof2PVI.Elevation:N3}");
                        }
                    }
                }

                trans.Commit();
            }
        }
    }
}

