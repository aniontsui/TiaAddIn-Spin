﻿using SpinXmlReader;
using SpinXmlReader.Block;
using SpinXmlReader.SimaticML;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Windows.Forms;
using System.Xml;
using TiaXmlReader.SimaticML;
using static SpinXmlReader.Block.Section;

namespace TiaXmlReader
{
    public partial class DBDuplicationForm : Form
    {
        private readonly SaveData saveData;
        private readonly Dictionary<string, uint> memberWordDict;

        public DBDuplicationForm(SaveData saveData)
        {
            InitializeComponent();

            this.saveData = saveData;
            this.dbXMLPathTextBox.Text = saveData.lastDBDuplicationFileName;
            this.replaceDBNameCheckBox.Checked = saveData.DBDuplicationReplaceDBName;
            this.startingDBNumberTextBox.Text = "" + saveData.DBDuplicationStartingNum;
            this.newNameTextBox.Text = saveData.DBDuplicationNewDBName;
            this.newDBNameTextBox.Text = saveData.DBDuplicationNewDBName;
            this.replacementList1TextBox.Text = saveData.DBDuplicationReplacementList1;
            this.replacementList2TextBox.Text = saveData.DBDuplicationReplacementList2;
            this.replacementList3TextBox.Text = saveData.DBDuplicationReplacementList3;

            this.memberWordDict = new Dictionary<string, uint>();
        }

        private void DBXMLPathTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml",
                CheckFileExists = true,
                FileName = saveData.lastDBDuplicationFileName
            };

            var result = fileDialog.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                saveData.lastDBDuplicationFileName = fileDialog.FileName;
                saveData.Save();

                dbXMLPathTextBox.Text = saveData.lastDBDuplicationFileName;
            }
        }

        private BlockAttributeList LoadAttributeListFromXMLFile()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(dbXMLPathTextBox.Text);
            var xmlConfiguration = SimaticMLParser.ParseXML(xmlDocument);

            BlockAttributeList attributeList = null;
            if (xmlConfiguration is BlockGlobalDB globlalDB)
            {
                attributeList = globlalDB.GetAttributes();
            }
            else if (xmlConfiguration is BlockInstanceDB instanceDB)
            {
                attributeList = instanceDB.GetAttributes();
            }

            if (attributeList == null)
            {
                throw new System.Exception("Imported xml is not a DB or is invalid.");
            }

            return attributeList;
        }

        private void AnalyzeFileButton_MouseClick(object sender, MouseEventArgs e)
        {
            if (string.IsNullOrEmpty(dbXMLPathTextBox.Text) || !File.Exists(dbXMLPathTextBox.Text))
            {
                return;
            }

            var section = LoadAttributeListFromXMLFile().ComputeSection(SectionTypeEnum.STATIC);

            memberWordDict.Clear();
            foreach (var member in section.GetItems())
            {
                AnalyzeMemberNames(member);
            }
            var wordList = memberWordDict.OrderBy(key => key.Value).Select(key => key.Key).ToList();

            memberReplacementComboBox.Items.Clear();
            memberReplacementComboBox.Items.AddRange(wordList.ToArray());
        }

        private void AnalyzeMemberNames(Member member)
        {
            var memberName = member.GetMemberName();
            if (!memberWordDict.ContainsKey(memberName))
            {
                memberWordDict.Add(memberName, 0);
            }

            memberWordDict[memberName] += 1;
            foreach (var subMember in member.GetItems())
            {
                this.AnalyzeMemberNames(subMember);
            }
        }

        private void ReplaceAndExportButton_MouseClick(object sender, MouseEventArgs e)
        {
            var startingDBNumber = uint.Parse(startingDBNumberTextBox.Text);

            var replacementArray1 = this.replacementList1TextBox.Text.Split('\n');
            var replacementArray2 = this.replacementList2TextBox.Text.Split('\n');
            var replacementArray3 = this.replacementList3TextBox.Text.Split('\n');
            for (int x = 0; x < replacementArray1.Length; x++)
            {
                var replacement1 = replacementArray1[x].Replace("\r", "");
                var replacement2 = (replacementArray2.Length > x ? replacementArray2[x] : "").Replace("\r", "");
                var replacement3 = (replacementArray3.Length > x ? replacementArray3[x] : "").Replace("\r", "");

                if (string.IsNullOrEmpty(replacement1) && string.IsNullOrEmpty(replacement2) && string.IsNullOrEmpty(replacement3))
                {
                    continue;
                }

                var attributeList = LoadAttributeListFromXMLFile();

                var section = attributeList.ComputeSection(SectionTypeEnum.STATIC);
                if (!string.IsNullOrEmpty(memberReplacementComboBox.Text))
                {
                    foreach (var member in section.GetItems())
                    {
                        var newMemberName = this.newNameTextBox.Text
                            .Replace("{replacement1}", replacement1)
                            .Replace("{replacement2}", replacement2)
                            .Replace("{replacement3}", replacement3);
                        ReplaceMemberNames(member, memberReplacementComboBox.Text, newMemberName);
                    }
                }

                if (replaceDBNameCheckBox.Checked)
                {
                    var newBlockDBName = this.newDBNameTextBox.Text
                        .Replace("{replacement1}", replacement1)
                        .Replace("{replacement2}", replacement2)
                        .Replace("{replacement3}", replacement3);
                    attributeList.SetBlockName(newBlockDBName);
                }

                attributeList.SetBlockNumber(startingDBNumber++);

                if(!string.IsNullOrEmpty(saveData.lastXMLExportPath))
                {
                    var xmlDocument = SimaticMLParser.CreateDocument();
                    xmlDocument.DocumentElement.AppendChild(attributeList.GetParentConfiguration().Generate(xmlDocument, new IDGenerator()));
                    xmlDocument.Save(saveData.lastXMLExportPath + "/DB" + attributeList.GetBlockNumber() + "_" + attributeList.GetBlockName() + ".xml");
                }
            }
        }
        private void ReplaceMemberNames(Member member, string toReplace, string replacement)
        {
            if (toReplace.ToLower() == member.GetMemberName().ToLower())
            {
                member.SetMemberName(replacement);
            }

            foreach (var subMember in member.GetItems())
            {
                this.ReplaceMemberNames(subMember, toReplace, replacement);
            }
        }

        private void ReplaceDBNameCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            saveData.DBDuplicationReplaceDBName = replaceDBNameCheckBox.Checked;
        }

        private void StartingDBNumberTextBox_TextChanged(object sender, System.EventArgs e)
        {
            if(uint.TryParse(startingDBNumberTextBox.Text, out uint num))
            {
                saveData.DBDuplicationStartingNum = num;
            }
        }

        private void NewDBNameTextBox_TextChanged(object sender, System.EventArgs e)
        {
            saveData.DBDuplicationNewDBName = newDBNameTextBox.Text;
        }

        private void NewNameTextBox_TextChanged(object sender, System.EventArgs e)
        {
            saveData.DBDuplicationNewMemberName = newNameTextBox.Text;
        }

        private void ReplacementList1TextBox_TextChanged(object sender, System.EventArgs e)
        {
            saveData.DBDuplicationReplacementList1 = replacementList1TextBox.Text;
        }

        private void ReplacementList2TextBox_TextChanged(object sender, System.EventArgs e)
        {
            saveData.DBDuplicationReplacementList2 = replacementList2TextBox.Text;
        }
        private void ReplacementList3TextBox_TextChanged(object sender, System.EventArgs e)
        {
            saveData.DBDuplicationReplacementList3 = replacementList3TextBox.Text;
        }

        private void DBDuplicationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            saveData.Save();
        }
    }
}
