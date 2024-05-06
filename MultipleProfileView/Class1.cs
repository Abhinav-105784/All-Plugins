using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using System.Collections;
using System.Collections.Generic;

[assembly: CommandClass(typeof(MultipleProfileView.DrawProfileView))]
namespace MultipleProfileView
{
    public class DrawProfileView
    {
        [CommandMethod("DrawProfileViews")]
        public void DrawProfile()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            CivilDocument cDoc = CivilApplication.ActiveDocument;
            editor.WriteMessage("\nSelect Alignments");
            PromptSelectionResult selectedAlignments = editor.GetSelection();
            if (selectedAlignments.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\nWrong Selection. Try again Later.");
                return;
            }
            editor.WriteMessage("\nSelect the surface");
            PromptEntityResult surfaceSelected = editor.GetEntity("\nSelect Existing surface");
            if (surfaceSelected.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\nWrong Selection. Re-Run the command");
                return;
            }
            PromptEntityResult surfaceSelected2 = editor.GetEntity("\nSelect Propsed surface");
            if (surfaceSelected2.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\nWrong Selection. Re-Run the command");
                return;
            }
            PromptStringOptions nameProfileView = new PromptStringOptions("\nName the Profile view")
              {
                  AllowSpaces=true,
              };
              PromptResult namePVResult = editor.GetString(nameProfileView);
              string name = namePVResult.StringResult;
              PromptIntegerOptions integerNumberProfileView = new PromptIntegerOptions("\n Initial number of Profile View");
              PromptIntegerResult intergerNumberPVResult = editor.GetInteger(integerNumberProfileView);
              int j = intergerNumberPVResult.Value;
              PromptStringOptions nameProfile = new PromptStringOptions("\nName the Existing Profile")
              {
                  AllowSpaces = true,
              };
              PromptResult namePResult = editor.GetString(nameProfile);
              string profileName = namePResult.StringResult;
              PromptIntegerOptions integerNumberProfile = new PromptIntegerOptions("\n Initial number of Existing Profile");
              int k = editor.GetInteger(integerNumberProfile).Value;
              PromptStringOptions nameProfile2 = new PromptStringOptions("\nName the Proposed Profile")
              {
                  AllowSpaces=true,
              };
              PromptResult namePResult2 = editor.GetString(nameProfile2);
              string profileName2 = namePResult2.StringResult;
              PromptIntegerOptions integerNumberProfile2 = new PromptIntegerOptions("\n Initial number of Proposed Profile");
              int l = editor.GetInteger(integerNumberProfile2).Value;
             
            SelectionSet set = selectedAlignments.Value;
           

           
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                
                ProfileViewBandSetStyleCollection styles = cDoc.Styles.ProfileViewBandSetStyles;
                foreach (ObjectId style in styles)
                {
                    ProfileViewBandSetStyle styleName = transaction.GetObject(style, OpenMode.ForRead) as ProfileViewBandSetStyle;
                    editor.WriteMessage($"\nAvailable bandset styles: {styleName.Name}");

                }


                    PromptStringOptions bandsetName = new PromptStringOptions("\nName the band set for profile.(ex, Pipe Data, EG-FG Elevations and Stations, Cut and Fill etc.)")
                    {
                        AllowSpaces = true
                    };
                    PromptResult bandSetNameResult = editor.GetString(bandsetName);
                    string bandset = bandSetNameResult.StringResult;
                ProfileStyleCollection profiles = cDoc.Styles.ProfileStyles;
                foreach(ObjectId profileStyle in profiles)
                {
                    ProfileStyle styleName = transaction.GetObject(profileStyle, OpenMode.ForRead) as ProfileStyle;
                    editor.WriteMessage($"\nAvailable Profile Styles : {styleName.Name}");
                }
                    PromptStringOptions profileStyleName = new PromptStringOptions("\nTell Existing Profile style.(ex. Existing Ground Profile, Basic, Design Profile etc.)")
                    {
                        AllowSpaces = true
                    };
                    PromptResult profileSlyeNameResult = editor.GetString(profileStyleName);
                    string proStyle = profileSlyeNameResult.StringResult;
                    PromptStringOptions profileStyleName2 = new PromptStringOptions("\nTell Proposed Profile style.(ex. Existing Ground Profile, Basic, Design Profile etc.)")
                    {
                        AllowSpaces = true
                    };
                    PromptResult profileSlyeNameResult2 = editor.GetString(profileStyleName2);
                    string proStyle2 = profileSlyeNameResult2.StringResult;
                    TinSurface surfaceSelected1 = transaction.GetObject(surfaceSelected.ObjectId, OpenMode.ForWrite) as TinSurface;
                    TinSurface surfaceSelected20 = transaction.GetObject(surfaceSelected2.ObjectId, OpenMode.ForWrite) as TinSurface;
                    foreach (SelectedObject obj in set)
                    {
                        if (obj.ObjectId.ObjectClass.DxfName.Contains("ALIGNMENT"))
                        {
                            Alignment alignment = transaction.GetObject(obj.ObjectId, OpenMode.ForWrite) as Alignment;

                            ObjectId profileViewStyle = cDoc.Styles.ProfileViewBandSetStyles[bandset];
                            ObjectId profileStyle = cDoc.Styles.ProfileStyles[proStyle];
                            ObjectId labelsetId = cDoc.Styles.LabelSetStyles.ProfileLabelSetStyles[0];
                            ObjectId profileStyle2 = cDoc.Styles.ProfileStyles[proStyle2];
                            string modifiedName = $"{name}--{j}";
                            j++;
                            string modifiedProfileName = $"{profileName}--{k}";
                            k++;
                            string modifiedProfileName2 = $"{profileName2}--{l}";
                            l++;
                            PromptPointOptions insertion = new PromptPointOptions($"\nSelect Insertion Point for Profile View of {alignment.Name}.");
                            PromptPointResult inserrtionResult = editor.GetPoint(insertion);
                            Point3d insertionPoint = inserrtionResult.Value;
                            Profile.CreateFromSurface(modifiedProfileName, alignment.ObjectId, surfaceSelected1.ObjectId, surfaceSelected1.LayerId, profileStyle, labelsetId);
                            Profile.CreateFromSurface(modifiedProfileName2, alignment.ObjectId, surfaceSelected20.ObjectId, surfaceSelected20.LayerId, profileStyle2, labelsetId);
                            ProfileView.Create(cDoc, modifiedName, profileViewStyle, alignment.ObjectId, insertionPoint);


                         




                        }
                    }
                
                transaction.Commit();
            }

        }
       
    }
}
