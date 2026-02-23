#region Using directives
using System;

using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.OmronEthernetIP;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.OmronFins;
using FTOptix.Alarm;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.WebUI;
using FTOptix.Modbus;
using FTOptix.MelsecFX3U;
using FTOptix.MelsecQ;
#endregion

public class IO_Setup : BaseNetLogic
{
    [ExportMethod]
    public void test_type()
    {        
         try
        {
            // Fetch the existing parent folder where new nodes will be added.
            var translationFolder = Project.Current.Get("Translations");
            if (translationFolder == null)
            {
                Log.Error("The 'Translations' folder does not exist.");
                return;
            }

            // Access an existing node (IO_List) in the translation folder.
            var ioList = translationFolder.Get("IO_List");

            // Check if the node exists and is the expected type.
            if (ioList != null && ioList is UAManagedCore.UAVariable)
            {
                Log.Info("Found the existing 'IO_List' variable.");
                var existingVariable = (UAManagedCore.UAVariable) ioList;

                // Create a new UAVariable with the same type (matching the existing one).
                var newVariable = InformationModel.Make<UAManagedCore.UAVariable>("NewIOList");

                // If necessary, replicate properties like BrowseName, DataType, etc.
                newVariable.BrowseName = "NewIOList";
                newVariable.DataType = existingVariable.DataType;
                
                // Add the new variable to the translation folder.
                translationFolder.Add(newVariable);

                Log.Info("New variable 'NewIOList' created and added successfully.");
            }
            else
            {
                Log.Error("The 'IO_List' node was not found or is not of type UAVariable.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error: {ex.Message}");
        }
    }
    
    [ExportMethod]
    public void test_dictionary()
    {             
        var translationFolder = Project.Current.Get("Translations");

        translationFolder.Remove(translationFolder.Get("IO_List"));
    }


    [ExportMethod]
    public void ImportIO_List()
    {
        // Reads the CSVPath property of the NetLogic. It is configured with datatype Absolute resource URI
        string csvPathVariable = LogicObject.GetVariable("CSVPath").Value;

        var csvPath = new ResourceUri(csvPathVariable).Uri;

        var modelFolder = Project.Current.Get("Model");
        var translationFolder = Project.Current.Get("Translations");

        if (csvPath.Length == 0 || !System.IO.File.Exists(csvPath))
        {
            Log.Error("CSV not found. Please configure the CSV to parse in the NetLogic configuration");
            return;
        }

        // Clear the related content of Model folder. 
        // It let you re-run the command without adding the same variable again.
        if (modelFolder.Get("IO_List") != null)
            modelFolder.Remove(modelFolder.Get("IO_List"));
        if (modelFolder.Get("_Types").Get("IO") != null)
            modelFolder.Get("_Types").Remove(modelFolder.Get("_Types").Get("IO"));

        //adding default type for IO variables
        var io_type = InformationModel.MakeObjectType("IO_Item");

        var v_text = InformationModel.MakeVariable("Text", OpcUa.DataTypes.LocalizedText);
        var v_out = InformationModel.MakeVariable("Out", OpcUa.DataTypes.Boolean);
        var v_enTest = InformationModel.MakeVariable("En_Test", OpcUa.DataTypes.Boolean);
        var v_status = InformationModel.MakeVariable("Status", OpcUa.DataTypes.Boolean);
        var v_cmd = InformationModel.MakeVariable("Command", OpcUa.DataTypes.Boolean);
        var v_empty = InformationModel.MakeVariable("Empty", OpcUa.DataTypes.Boolean);
        var v_enMan = InformationModel.MakeVariable("En_Manual", OpcUa.DataTypes.Boolean);
        var v_Man = InformationModel.MakeVariable("Manual", OpcUa.DataTypes.Boolean);
        var v_group = InformationModel.MakeVariable("Group", OpcUa.DataTypes.LocalizedText);

        io_type.Add(v_text);
        io_type.Add(v_out);
        io_type.Add(v_enTest);
        io_type.Add(v_status);
        io_type.Add(v_cmd);
        io_type.Add(v_empty);
        io_type.Add(v_enMan);
        io_type.Add(v_Man);
        io_type.Add(v_group);

        //adds <IO_Item> type
        modelFolder.Get("_Types").Add(InformationModel.Make<FTOptix.Core.Folder>("IO"));
        modelFolder.Get("_Types").Get("IO").Add(io_type);

        modelFolder.Add(InformationModel.Make<FTOptix.Core.Folder>("IO_List"));

        // Clear and re-adds IO Dictionary.
        //translationFolder.Remove("IO_List");


        //nodename,lang_key,lang_primary,in_out_skip,var_signal,var_out_en,var_out_cmd

        //string[] lines = System.IO.File.ReadAllLines(csvPath);
        
        StuffMakeNewDictionary("MyNewDictionary", new string[6] { "", "it-IT", "en-US", "fr-FR", "es-ES","de-DE" }, translationFolder);
    }



/*
        foreach (string line in lines)
        {
            var tokens = line.Split(',');

            var



/*
            if (tokens[0] == "variable")
            {
                NodeId dataType = tokens[2] == "boolean" ? OpcUa.DataTypes.Boolean : OpcUa.DataTypes.UInt32;

                var newVariable = InformationModel.MakeVariable(tokens[1], dataType);
                modelFolder.Add(newVariable);
            }
            else if (tokens[0] == "alarm")
            {
                // Searches the variable to link before creating the alarm
                var inputVariable = modelFolder.GetVariable(tokens[3]);

                AlarmController newAlarm;

                if (tokens[2] == "digital")
                    newAlarm = InformationModel.MakeObject<DigitalAlarm>(tokens[1]);
                else
                    newAlarm = InformationModel.MakeObject<ExclusiveLevelAlarmController>(tokens[1]);

                // Creates a dynamic link from the InputValue property of the Alarm to the actual variable
                newAlarm.InputValueVariable.SetDynamicLink(inputVariable);
                alarmsFolder.Add(newAlarm);
            }
            
        }
    */

    private void StuffMakeNewDictionary(string browseName, string[]languages, IUANode dictionaryOwner) 
    {
        if (dictionaryOwner == null || languages.Length <= 0 || string.IsNullOrEmpty(browseName))
            return;
        if (languages[0] != "")
        {
            // The first column needs to be empty as it's the keys list
            string[] firstColumnEmpty = new string[1] {""};
            // Add the languages to the other columns
            languages = firstColumnEmpty.Concat(languages).ToArray();
        }
        List<string> languageVerifiedToAdd = new List<string>();
        
        foreach (string language in languages) 
        {
            try
            {
                if (CultureInfo.GetCultureInfo(language, false) != null)
                    languageVerifiedToAdd.Add(language);
            }
            catch 
            {  
                // In case of culture identifier is not valid, skip to add to dictionary          
            }
        }
        string[,] newDictionaryValues = new string[1, languageVerifiedToAdd.Count];
        for (int i = 0; i < languageVerifiedToAdd.Count; i++) 
        {
            newDictionaryValues[0,i] = languageVerifiedToAdd[i];
        }
        // Create the new dictionary
        IUAVariable newDictionary = InformationModel.MakeVariable(
            browseName, 
            OpcUa.DataTypes.String, 
            FTOptix.Core.VariableTypes.LocalizationDictionary, 
            new uint[2] { 
                (uint)newDictionaryValues.GetLength(0), 
                (uint)newDictionaryValues.GetLength(1) 
            });
        // Set the dictionary content
        newDictionary.Value = new UAValue(newDictionaryValues);
        // Check if the dictionary does not exist already, then add it
        if (dictionaryOwner.Get(browseName) == null) 
            dictionaryOwner.Add(newDictionary);
    }
}
