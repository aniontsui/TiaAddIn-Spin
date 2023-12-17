﻿using DocumentFormat.OpenXml.Office2010.ExcelAc;
using SpinXmlReader;
using SpinXmlReader.Block;
using SpinXmlReader.SimaticML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TiaXmlReader.SimaticML.Enums;
using TiaXmlReader.Utility;

namespace TiaXmlReader.SimaticML.BlockFCFB.FlagNet.AccessNamespace
{
    public class Access : XmlNodeConfiguration
    {
        public const string NODE_NAME = "Access";

        private readonly XmlAttributeConfiguration uid;
        private readonly XmlAttributeConfiguration scope;

        private readonly XmlNodeListConfiguration<Component> symbol; //FOR GLOBAL AND LOCAL VARIABLES

        private readonly XmlNodeConfiguration constant;
        private readonly XmlAttributeConfiguration constantName;     //FOR LOCAL AND GLOBAL CONSTANT
        private readonly XmlNodeConfiguration constantType;          //FOR LITERAL CONSTANT (NUMBER WRITTEN DIRECTLY)
        private readonly XmlNodeConfiguration constantValue;         //FOR LITERAL CONSTANT (NUMBER WRITTEN DIRECTLY) AND TYPED_CONSTANT

        private readonly XmlNodeConfiguration label;          //Not implemented
        private readonly XmlAttributeConfiguration labelName; //Not implemented

        public Access(CompileUnit compileUnit = null) : base(Access.NODE_NAME)
        {
            if (compileUnit != null)
            {
                compileUnit.AddAccess(this);
            }

            //==== INIT CONFIGURATION ====
            scope = this.AddAttribute("Scope", required: true);
            uid = this.AddAttribute("UId"); //In case the access is innested inside a Component (For array access modifier) UId is not required.         
            if (compileUnit != null) //If a compile unit has been added, it means it needs an UId to be associated with a Wire.
            {
                this.uid.SetValue("" + compileUnit.LocalIDGenerator.GetNext());
            }

            symbol = this.AddNodeList("Symbol", Component.CreateComponent);

            constant = this.AddNode("Constant");
            constantName = constant.AddAttribute("Name");
            constantType = constant.AddNode("ConstantType");
            constantValue = constant.AddNode("ConstantValue");

            label = this.AddNode("Label");
            labelName = this.label.AddAttribute("Name");
            //==== INIT CONFIGURATION ====
        }

        public uint GetUId()
        {
            return uint.TryParse(this.uid.GetValue(), out uint uid) ? uid : 0;
        }

        public Access SetVariableScope(SimaticVariableScope scope)
        {
            this.scope.SetValue(scope.GetSimaticMLString());
            return this;
        }

        public SimaticVariableScope GetVariableScope()
        {
            foreach (SimaticVariableScope variableScope in Enum.GetValues(typeof(SimaticVariableScope)))
            {
                if (scope.GetValue() == variableScope.GetSimaticMLString())
                {
                    return variableScope;
                }
            }

            throw new Exception("Invalid VariableScope for " + scope.GetValue());
        }

        public string GetAddress()
        {
            var componentList = symbol.GetItems()
                                    .Select(x => x.GetComponentName())
                                    .ToList();
            return SimaticMLUtil.JoinComponentsIntoAddress(componentList);
        }

        public void SetAddress(string address)
        {
            var addressComponentList = SimaticMLUtil.SplitFullAddressIntoComponents(address);

            symbol.GetItems().Clear();
            foreach (var component in Access.ParseAddressComponents(addressComponentList))
            {
                symbol.GetItems().Add(component);
            }
        }

        private static List<Component> ParseAddressComponents(List<SimaticAddressComponent> addressComponentList)
        {
            var componentList = new List<Component>();
            foreach (var addressComponent in addressComponentList)
            {
                var component = new Component()
                        .SetComponentName(addressComponent.GetName());

                foreach (var arrayIndex in addressComponent.GetArrayIndexes())
                {
                    if (component.GetSliceAccessModifier() != "Array")
                    {
                        component.SetSliceAccessModifier("Array");
                    }

                    var access = new Access();
                    foreach (var arrayIndexComponent in Access.ParseAddressComponents(arrayIndex.GetComponents()))
                    {
                        access.symbol.GetItems().Add(arrayIndexComponent);
                    }
                    component.GetItems().Add(access);
                }

                componentList.Add(component);
            }
            return componentList;
        }

        public string GetConstantName()
        {
            return constantName.GetValue();
        }

        public Access SetConstantName(string constantName)
        {
            this.constantName.SetValue(constantName);
            return this;
        }

        public SimaticDataType GetConstantType()
        {
            return SimaticDataTypeUtil.GetFromSimaticMLString(this.constantType.GetInnerText());
        }

        public Access SetConstantType(SimaticDataType dataType)
        {
            this.constantType.SetInnerText(dataType.GetSimaticMLString());
            return this;
        }

        public string GetConstantValue()
        {
            return constantValue.GetInnerText();
        }

        public void SetConstantValue(string constantValue)
        {
            this.constantValue.SetInnerText(constantValue);
        }
    }

    //In case of accessing a multi depth array (Like a Matrix) can have multiple access inside for the Array sliceAccessModifier.
    public class Component : XmlNodeListConfiguration<Access>
    {
        public const string NODE_NAME = "Component";
        public static Component CreateComponent(XmlNode node)
        {
            return node.Name == Component.NODE_NAME ? new Component() : null;
        }

        public static Access CreateAccess(XmlNode node)
        {
            return node.Name == Access.NODE_NAME ? new Access() : null;
        }

        private readonly XmlAttributeConfiguration componentName;
        private readonly XmlAttributeConfiguration sliceAccessModifier;

        public Component(string value = "") : base(Component.NODE_NAME, Component.CreateAccess, required: true)
        {
            //==== INIT CONFIGURATION ====
            componentName = this.AddAttribute("Name", required: true, value: value);
            sliceAccessModifier = this.AddAttribute("SliceAccessModifier");
            //==== INIT CONFIGURATION ====
        }

        public string GetComponentName()
        {
            return componentName.GetValue();
        }

        public Component SetComponentName(string name)
        {
            this.componentName.SetValue(name);
            return this;
        }

        public string GetSliceAccessModifier()
        {
            return sliceAccessModifier.GetValue();
        }

        public Component SetSliceAccessModifier(string sliceAccessModifier)
        {
            this.sliceAccessModifier.SetValue(sliceAccessModifier);
            return this;
        }
    }
}
