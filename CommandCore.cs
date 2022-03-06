using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Command Settings", menuName = "menu/Command Settings", order = 1)]
public class CommandCore : ScriptableObject
{
    [SerializeField]
    char delimiter_;
    public char delimiter { get { return delimiter_; } }

    [SerializeField]
    string defaultStr;
    [SerializeField]
    List<string> commands;
    public int commandCount { get { return commands.Count; } }
    public string GetCommand(int index)
    {
        string str = delimiter_.ToString();
        if (index == -1)
        {
            str += defaultStr;
            return str;
        }
        else if (index < -1)
        {
            str += "err";
            return str;
        }

        str += commands[index];
        return str;
    }

    // Decode raw data.
    // Separate data and commands.
    public static List<CommandData> Decode(CommandCore core, string rawData)
    {
        List<CommandData> data = new List<CommandData>();
        string dataRemain = rawData, target = core.delimiter.ToString() + core.delimiter.ToString();
        List<int> notDelimiterChar = new List<int>();
        bool loop = true;
        int slicingOffset = 0;

        // When commands doesn't exist in data.
        if (dataRemain.IndexOf(core.delimiter) == -1)
        {
            data.Add(new CommandData(-1, dataRemain));
            return data;
        }

        // Check delimiter char but not command.
        while (dataRemain.Contains(target))
        {
            notDelimiterChar.Add(dataRemain.IndexOf(target) + slicingOffset);
            dataRemain = dataRemain.Substring(0, notDelimiterChar[notDelimiterChar.Count - 1]) + dataRemain.Substring(notDelimiterChar[notDelimiterChar.Count - 1] + 2);
            slicingOffset += 2;
        }

        slicingOffset = 0;
        while (loop)
        {
            for (int i = -1; i < core.commandCount; i++)
            {
                target = core.GetCommand(i);

                // When commands exist in data.
                if (dataRemain.StartsWith(target))
                {
                    // Split commands, result can have same commands repeat(it needs to reassemble).
                    string[] splits = dataRemain.Split(new string[] { target }, StringSplitOptions.RemoveEmptyEntries);
                    dataRemain = "";
                    for (int j = 0; j < splits.Length; j++)
                        dataRemain += splits[j] + target;
                    dataRemain = dataRemain.Substring(0, dataRemain.LastIndexOf(target)) + dataRemain.Substring(dataRemain.LastIndexOf(target) + target.Length);

                    // Check still commands are here.
                    int index = dataRemain.IndexOf(core.delimiter);

                    // Job done.
                    if (index == -1)
                    {
                        data.Add(new CommandData(i, dataRemain));
                        loop = false;
                        break;
                    }

                    // Still commands exist in data.
                    data.Add(new CommandData(i, dataRemain.Substring(0, index)));
                    dataRemain = dataRemain.Substring(index);
                    break;
                }
            }

            // Check delimiter char but not command.
            int lastDataSize = data[data.Count - 1].text.Length;
            foreach (int index in notDelimiterChar)
            {
                if ((index - target.Length) < 0)
                {
                    Debug.Log("Delimiter in command error!");
                    return null;
                }

                if ((index - target.Length - data[data.Count - 1].text.Length) < 0)
                    slicingOffset++;
            }

            for (int i = 0; i < slicingOffset; i++)
            {
                data[data.Count - 1].text = data[data.Count - 1].text.Insert((notDelimiterChar[0] - target.Length), core.delimiter.ToString());
                notDelimiterChar.RemoveAt(0);
            }

            for (int i = slicingOffset; i < notDelimiterChar.Count; i++)
                notDelimiterChar[i] -= (target.Length + data[data.Count - 1].text.Length);
        }

        return data;
    }

    public static string Encode(CommandCore core, List<CommandData> data)
    {
        string rawData = "";
        for (int i = 0; i < data.Count; i++)
        {
            // Check delimiter char but not command.
            if (data[i].text.Contains(core.delimiter.ToString()))
            {
                rawData += core.GetCommand(data[i].command) + data[i].text.Replace(core.delimiter.ToString(), core.delimiter.ToString() + core.delimiter.ToString());
                continue;
            }

            rawData += core.GetCommand(data[i].command) + data[i].text;
        }

        return rawData;
    }
}
