// NReflect - Easy assembly reflection
// Copyright (C) 2010-2011 Malte Ried
//
// This file is part of NReflect.
//
// NReflect is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// NReflect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with NReflect. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NReflect;
using NReflect.Modifier;
using NReflect.NRAttributes;
using NReflect.NREntities;
using NReflect.NRMembers;
using NReflect.NRParameters;
using ParameterModifier = NReflect.Modifier.ParameterModifier;

namespace NReflectRunner
{
  /// <summary>
  /// This class implements the <see cref="IVisitor"/> interface to print
  /// the contents of a <see cref="NRAssembly"/> onto to console.
  /// </summary>
  public class PrintVisitor : IVisitor
  {
    // ========================================================================
    // Fields

    #region === Fields

    private readonly TextWriter writer;

    private int indent;

    #endregion

    // ========================================================================
    // Con- / Destruction

    #region === Con- / Destruction

    /// <summary>
    /// Initializes a new instance of <see cref="PrintVisitor"/>.
    /// </summary>
    public PrintVisitor() : this(Console.Out)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PrintVisitor"/>.
    /// </summary>
    /// <param name="writer">This <see cref="TextWriter"/> will be used for output.</param>
    public PrintVisitor(TextWriter writer)
    {
      this.writer = writer;
      indent = 0;
    }

    #endregion

    // ========================================================================
    // Methods

    #region === Methods

    public void Visit(NRAssembly nrAssembly)
    {
      OutputLine("Assembly " + nrAssembly.FullName);
      OutputLine("  Source " + nrAssembly.Source);
      VisitAttributes(nrAssembly);
      OutputLine("");
      PrintEntities(nrAssembly);
    }

    public void Visit(NRClass nrClass)
    {
      VisitAttributes(nrClass);
      Output(ToString(nrClass.AccessModifier) + ToString(nrClass.ClassModifier) + "class " + nrClass.Name + GetGenericDefinition(nrClass));
      PrintBaseTypeAndInterfaces(nrClass);
      VisitTypeParameters(nrClass);
      OutputLine("");
      OutputLine("{");
      indent++;
      foreach (NRField nrField in nrClass.Fields)
      {
        nrField.Accept(this);
      }
      foreach (NRProperty nrProperty in nrClass.Properties)
      {
        nrProperty.Accept(this);
      }
      foreach (NRConstructor nrConstructor in nrClass.Constructors)
      {
        nrConstructor.Accept(this);
      }
      foreach (NRMethod nrMethod in nrClass.Methods)
      {
        nrMethod.Accept(this);
      }
      foreach (NROperator nrOperator in nrClass.Operators)
      {
        nrOperator.Accept(this);
      }
      foreach (NREvent nrEvent in nrClass.Events)
      {
        nrEvent.Accept(this);
      }
      foreach (NRTypeBase nrTypeBase in nrClass.NestedTypes)
      {
        nrTypeBase.Accept(this);
      }
      indent--;
      OutputLine("}");
    }

    public void Visit(NRInterface nrInterface)
    {
      VisitAttributes(nrInterface);
      Output(ToString(nrInterface.AccessModifier) + "interface " + nrInterface.Name + GetGenericDefinition(nrInterface));
      PrintImplementedInterfaces(nrInterface);
      VisitTypeParameters(nrInterface);
      OutputLine("");
      OutputLine("{");
      indent++;
      foreach (NRProperty nrProperty in nrInterface.Properties)
      {
        nrProperty.Accept(this);
      }
      foreach (NREvent nrEvent in nrInterface.Events)
      {
        nrEvent.Accept(this);
      }
      foreach(NRMethod nrMethod in nrInterface.Methods)
      {
        nrMethod.Accept(this);
      }
      indent--;
      OutputLine("}");
    }

    public void Visit(NRDelegate nrDelegate)
    {
      VisitAttributes(nrDelegate);
      Output(ToString(nrDelegate.AccessModifier) + "delegate " + ToString(nrDelegate.ReturnType) + " " + nrDelegate.Name + GetGenericDefinition(nrDelegate) + "(");
      PrintParameters(nrDelegate.Parameters);
      Output(")", 0);
      VisitTypeParameters(nrDelegate);
      OutputLine("");
    }

    public void Visit(NRStruct nrStruct)
    {
      VisitAttributes(nrStruct);
      Output(ToString(nrStruct.AccessModifier) + "struct " + nrStruct.Name + GetGenericDefinition(nrStruct));
      PrintBaseTypeAndInterfaces(nrStruct);
      VisitTypeParameters(nrStruct);
      OutputLine("");
      OutputLine("{");
      indent++;
      foreach (NRField nrField in nrStruct.Fields)
      {
        nrField.Accept(this);
      }
      foreach (NRProperty nrProperty in nrStruct.Properties)
      {
        nrProperty.Accept(this);
      }
      foreach (NRConstructor nrConstructor in nrStruct.Constructors)
      {
        nrConstructor.Accept(this);
      }
      foreach (NRMethod nrMethod in nrStruct.Methods)
      {
        nrMethod.Accept(this);
      }
      foreach (NROperator nrOperator in nrStruct.Operators)
      {
        nrOperator.Accept(this);
      }
      foreach (NREvent nrEvent in nrStruct.Events)
      {
        nrEvent.Accept(this);
      }
      foreach (NRTypeBase nrTypeBase in nrStruct.NestedTypes)
      {
        nrTypeBase.Accept(this);
      }
      indent--;
      OutputLine("}");
    }

    public void Visit(NREnum nrEnum)
    {
      VisitAttributes(nrEnum);
      OutputLine(ToString(nrEnum.AccessModifier) + "enum " + nrEnum.Name);
      OutputLine("{");
      indent++;
      foreach (NREnumValue nrEnumValue in nrEnum.Values)
      {
        nrEnumValue.Accept(this);
      }
      indent--;
      OutputLine("}");
    }

    public void Visit(NRField nrField)
    {
      VisitAttributes(nrField);
      string value = "";
      if(nrField.IsConstant)
      {
        value = ": " + nrField.InitialValue;
      }
      OutputLine(ToString(nrField.AccessModifier) + ToString(nrField.FieldModifier) + ToString(nrField.Type) + " " + nrField.Name + value);
    }

    public void Visit(NRProperty nrProperty)
    {
      VisitAttributes(nrProperty);
      string methods = "";
      if(nrProperty.HasGetter)
      {
        methods = ToString(nrProperty.GetterModifier) + "get ";
      }
      if(nrProperty.HasSetter)
      {
        methods += ToString(nrProperty.SetterModifier) + "set ";
      }
      Output(ToString(nrProperty.AccessModifier) + ToString(nrProperty.OperationModifier) + ToString(nrProperty.Type) + " " +
             nrProperty.Name);
      if(nrProperty.Parameters.Count > 0)
      {
        Output("[", 0);
        PrintParameters(nrProperty.Parameters);
        Output("]", 0);
      }
      OutputLine(" { " + methods + "}", 0);
    }

    public void Visit(NRMethod nrMethod)
    {
      VisitAttributes(nrMethod);
      VisitReturnValueAttributes(nrMethod);
      Output(ToString(nrMethod.AccessModifier) + ToString(nrMethod.OperationModifier) + ToString(nrMethod.Type) + " " + nrMethod.Name + GetGenericDefinition(nrMethod) + "(");
      PrintParameters(nrMethod.Parameters, nrMethod.IsExtensionMethod);
      Output(")", 0);
      VisitTypeParameters(nrMethod);
      OutputLine("", 0);
    }

    public void Visit(NROperator nrOperator)
    {
      VisitAttributes(nrOperator);
      VisitReturnValueAttributes(nrOperator);
      string returnType = ToString(nrOperator.Type);
      if (!String.IsNullOrWhiteSpace(returnType))
      {
        returnType = returnType + " ";
      }
      Output(ToString(nrOperator.AccessModifier) + ToString(nrOperator.OperationModifier) + returnType + nrOperator.Name + "(");
      PrintParameters(nrOperator.Parameters);
      OutputLine(")", 0);
    }

    public void Visit(NRConstructor nrConstructor)
    {
      VisitAttributes(nrConstructor);
      Output(ToString(nrConstructor.AccessModifier) + ToString(nrConstructor.OperationModifier) + nrConstructor.Name + "(");
      PrintParameters(nrConstructor.Parameters);
      OutputLine(")", 0);
    }

    public void Visit(NREvent nrEvent)
    {
      VisitAttributes(nrEvent);
      Output(ToString(nrEvent.AccessModifier) + "event " + ToString(nrEvent.Type) + " " + nrEvent.Name + "(");
      PrintParameters(nrEvent.Parameters);
      OutputLine(")", 0);
    }

    public void Visit(NRParameter nrParameter)
    {
      foreach(NRAttribute nrAttribute in nrParameter.Attributes)
      {
        Output(GetAttribute(nrAttribute) + " ", 0);
      }
      Output(ToString(nrParameter.ParameterModifier) + ToString(nrParameter.Type) + " " + nrParameter.Name, 0);
      if(nrParameter.ParameterModifier == ParameterModifier.Optional)
      {
        Output(" = " + nrParameter.DefaultValue, 0);
      }
    }

    public void Visit(NRTypeParameter nrTypeParameter)
    {
      if (!nrTypeParameter.IsStruct && !nrTypeParameter.IsClass && nrTypeParameter.BaseTypes.Count <= 0 && !nrTypeParameter.IsConstructor && !nrTypeParameter.IsIn && !nrTypeParameter.IsOut)
      {
        return;
      }

      StringBuilder result = new StringBuilder(" where " + nrTypeParameter.Name + " :");
      if (nrTypeParameter.IsStruct)
      {
        result.Append(" struct, ");
      }
      else if(nrTypeParameter.IsClass)
      {
        result.Append(" class, ");
      }
      foreach(string baseType in nrTypeParameter.BaseTypes)
      {
        result.Append(" " + baseType + ", ");
      }
      if(nrTypeParameter.IsConstructor)
      {
        result.Append(" new(), ");
      }
      result.Length -= 2;
      Output(result.ToString(), 0);
    }

    public void Visit(NREnumValue nrEnumValue)
    {
      VisitAttributes(nrEnumValue);
      string value = "";
      if(!String.IsNullOrWhiteSpace(nrEnumValue.Value))
      {
        value = " = " + nrEnumValue.Value;
      }
      OutputLine(nrEnumValue.Name + value + ",");
    }

    public void Visit(NRAttribute nrAttribute)
    {
      OutputLine(GetAttribute(nrAttribute));
    }

    public void Visit(NRModule nrModule)
    {
      OutputLine("Module: " + nrModule.Name);
      VisitAttributes(nrModule);
      PrintEntities(nrModule);
      OutputLine("The module contains the following fields:");
      foreach(NRField nrField in nrModule.Fields)
      {
        nrField.Accept(this);
      }
      OutputLine("The module contains the following methods:");
      foreach(NRMethod nrMethod in nrModule.Methods)
      {
        nrMethod.Accept(this);
      }
    }

    /// <summary>
    /// Visits all type parameters of the given <see cref="IGeneric"/>.
    /// </summary>
    /// <param name="generic">The type parameters of this <see cref="IGeneric"/>
    /// gets visited.</param>
    private void VisitTypeParameters(IGeneric generic)
    {
      foreach(NRTypeParameter nrTypeParameter in generic.GenericTypes)
      {
        nrTypeParameter.Accept(this);
      }
    }

    /// <summary>
    /// Visits all attributes of the given <see cref="IAttributable"/>.
    /// </summary>
    /// <param name="attributable">The attributes of this <see cref="IAttributable"/>
    /// gets visited.</param>
    private void VisitAttributes(IAttributable attributable)
    {
      foreach(NRAttribute nrAttribute in attributable.Attributes)
      {
        nrAttribute.Accept(this);
      }
    }

    /// <summary>
    /// Visits all return attributes of the given <see cref="NRReturnValueOperation"/>.
    /// </summary>
    /// <param name="nrReturnValueOperation">The attributes of this <see cref="NRReturnValueOperation"/>
    /// gets visited.</param>
    private void VisitReturnValueAttributes(NRReturnValueOperation nrReturnValueOperation)
    {
      foreach(NRAttribute nrAttribute in nrReturnValueOperation.ReturnValueAttributes)
      {
        OutputLine(GetAttribute(nrAttribute, true));
      }
    }

    /// <summary>
    /// Prints the base type and all implemented interfaces of the given <see cref="NRSingleInheritanceType"/>.
    /// </summary>
    /// <param name="nrSingleInheritanceType">An <see cref="NRSingleInheritanceType"/> to take the base type and interfaces from.</param>
    private void PrintBaseTypeAndInterfaces(NRSingleInheritanceType nrSingleInheritanceType)
    {
      if(nrSingleInheritanceType.BaseType == null && nrSingleInheritanceType.ImplementedInterfaces.Count == 0)
      {
        return;
      }
      Output(" : ");
      if(nrSingleInheritanceType.BaseType != null)
      {
        Output(nrSingleInheritanceType.BaseType);
      }
      foreach(string implementedInterface in nrSingleInheritanceType.ImplementedInterfaces)
      {
        Output(", " + implementedInterface);
      }
    }

    /// <summary>
    /// Prints all implemented interfaces of the given <see cref="NRCompositeType"/>.
    /// </summary>
    /// <param name="nrCompositeType">An <see cref="NRCompositeType"/> to take the interfaces from.</param>
    private void PrintImplementedInterfaces(NRCompositeType nrCompositeType)
    {
      if (nrCompositeType.ImplementedInterfaces.Count == 0)
      {
        return;
      }
      Output(" : ");
      StringBuilder result = new StringBuilder();
      foreach (string implementedInterface in nrCompositeType.ImplementedInterfaces)
      {
        result.Append(implementedInterface + ", ");
      }
      result.Length -= 2;
      Output(result.ToString());
    }

    /// <summary>
    /// Prints all entities of the given <see cref="IEntityContainer"/>.
    /// </summary>
    /// <param name="entityContainer">An <see cref="IEntityContainer"/> containing the enities to print.</param>
    private void PrintEntities(IEntityContainer entityContainer)
    {
      PrintEntities("delegates", entityContainer.Delegates);
      PrintEntities("interfaces", entityContainer.Interfaces);
      PrintEntities("structs", entityContainer.Structs);
      PrintEntities("enums", entityContainer.Enums);
      PrintEntities("classes", entityContainer.Classes);
    }

    /// <summary>
    /// Prints the entities.
    /// </summary>
    /// <param name="entityType">A string describing the type of the entities to print.</param>
    /// <param name="types">A list of entities to print.</param>
    private void PrintEntities(string entityType, IEnumerable<NRTypeBase> types)
    {
      OutputLine(String.Format("Reflected {0}:", entityType));
      foreach (IVisitable type in types)
      {
        type.Accept(this);
        Console.WriteLine();
      }
    }

    /// <summary>
    /// Prints the parameters.
    /// </summary>
    /// <param name="nrParameters">A list of the parameters to print.</param>
    /// <param name="fromExtensionMethod">Set to true if the parameters of an extension method should be printed.</param>
    private void PrintParameters(List<NRParameter> nrParameters, bool fromExtensionMethod = false)
    {
      for(int i = 0; i < nrParameters.Count; i++)
      {
        if(i == 0 && fromExtensionMethod)
        {
          Output("this ", 0);
        }
        nrParameters[i].Accept(this);
        if(i < nrParameters.Count - 1)
        {
          Output(", ", 0);
        }
      }
    }

    private void Output(string text)
    {
      Output(text, indent);
    }

    private void Output(string text, int indention)
    {
      writer.Write("{0," + indention * 2 + "}{1}", "", text);
    }

    private void OutputLine(string text)
    {
      OutputLine(text, indent);
    }

    private void OutputLine(string text, int indention)
    {
      writer.WriteLine("{0," + indention * 2 + "}{1}", "", text);
    }

    /// <summary>
    /// Returns a readable string containing the <see cref="AccessModifier"/>.
    /// </summary>
    /// <param name="accessModifier">The <see cref="AccessModifier"/> to convert to a string.</param>
    /// <returns>The converted <see cref="AccessModifier"/></returns>
    private static string ToString(AccessModifier accessModifier)
    {
      if (accessModifier == AccessModifier.Default)
      {
        return "";
      }
      return accessModifier.ToString().ToLower() + " ";
    }

    /// <summary>
    /// Returns a readable string containing the <see cref="ClassModifier"/>.
    /// </summary>
    /// <param name="classModifier">The <see cref="ClassModifier"/> to convert to a string.</param>
    /// <returns>The converted <see cref="ClassModifier"/></returns>
    private static string ToString(ClassModifier classModifier)
    {
      if (classModifier == ClassModifier.None)
      {
        return "";
      }
      return classModifier.ToString().ToLower() + " ";
    }

    /// <summary>
    /// Returns a readable string containing the <see cref="ParameterModifier"/>.
    /// </summary>
    /// <param name="parameterModifier">The <see cref="ParameterModifier"/> to convert to a string.</param>
    /// <returns>The converted <see cref="ParameterModifier"/></returns>
    private static string ToString(ParameterModifier parameterModifier)
    {
      if (parameterModifier == ParameterModifier.In)
      {
        return "";
      }
      return parameterModifier.ToString().ToLower() + " ";
    }

    /// <summary>
    /// Returns a readable string containing the <see cref="OperationModifier"/>.
    /// </summary>
    /// <param name="operationModifier">The <see cref="OperationModifier"/> to convert to a string.</param>
    /// <returns>The converted <see cref="OperationModifier"/></returns>
    private static string ToString(OperationModifier operationModifier)
    {
      if (operationModifier == OperationModifier.None)
      {
        return "";
      }
      return operationModifier.ToString().ToLower() + " ";
    }

    /// <summary>
    /// Returns a readable string containing the <see cref="FieldModifier"/>.
    /// </summary>
    /// <param name="fieldModifier">The <see cref="FieldModifier"/> to convert to a string.</param>
    /// <returns>The converted <see cref="FieldModifier"/></returns>
    private static string ToString(FieldModifier fieldModifier)
    {
      if (fieldModifier == FieldModifier.None)
      {
        return "";
      }
      return fieldModifier.ToString().ToLower() + " ";
    }

    /// <summary>
    /// Returns a readable string containing the <see cref="NRType"/>.
    /// </summary>
    /// <param name="nrType">The <see cref="NRType"/> to convert to a string.</param>
    /// <returns>The converted <see cref="NRType"/></returns>
    private string ToString(NRType nrType)
    {
      return nrType.IsDynamic ? "dynamic" : nrType.Type;
    }

    /// <summary>
    /// Gets a string representing the C#-Code for an attribute.
    /// </summary>
    /// <param name="nrAttribute">The attribute to show.</param>
    /// <param name="returnAttribute">Set to true if the attribute is taken from a return value.</param>
    /// <returns>A string representing the C#-Code for an attribute.</returns>
    private static string GetAttribute(NRAttribute nrAttribute, bool returnAttribute = false)
    {
      StringBuilder result = new StringBuilder("[");
      if(returnAttribute)
      {
        result.Append("return: ");
      }
      result.Append(nrAttribute.Name.EndsWith("Attribute")
                      ? nrAttribute.Name.Substring(0, nrAttribute.Name.Length - "Attribute".Length)
                      : nrAttribute.Name);
      if(nrAttribute.Values.Count > 0 || nrAttribute.NamedValues.Count > 0)
      {
        result.Append("(");
        foreach(NRAttributeValue value in nrAttribute.Values)
        {
          result.Append(GetAttributeValueString(value) + ", ");
        }
        foreach(string key in nrAttribute.NamedValues.Keys)
        {
          result.AppendFormat("{0} = {1}, ", key, GetAttributeValueString(nrAttribute.NamedValues[key]));
        }
        result.Length -= 2;
        result.Append(")");
      }
      result.Append("]");
      return result.ToString();
    }

    /// <summary>
    /// Gets the C#-Code representing the value of the attribute.
    /// </summary>
    /// <param name="value">The attribute value to get the code for.</param>
    /// <returns>The C#-Code for the value.</returns>
    private static string GetAttributeValueString(NRAttributeValue value)
    {
      if(value.Type == "System.String")
      {
        return "\"" + value.Value + "\"";
      }
      if(value.Type == "System.Type")
      {
        return "typeof(" + value.Value + ")";
      }
      Type type = Type.GetType(value.Type, false);
      if(type != null && type.IsEnum)
      {
        try
        {
          string format = Enum.Format(type, value.Value, "F");
          StringBuilder result = new StringBuilder();
          foreach (string constant in format.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
          {
            result.Append(type.FullName + "." + constant + " || ");
          }
          if (result.Length > 0)
          {
            result.Length -= 4;
          }

          return result.ToString();
        }
        catch(Exception)
        {
          return value.Value.ToString();
        }
      }
      return value.Value.ToString();
    }

    /// <summary>
    /// Returns a string containing the type parameter definitions.
    /// </summary>
    /// <param name="generic">An instance of <see cref="IGeneric"/> to take the generic parameters from.</param>
    /// <returns>A string containing the type parameter definitions.</returns>
    private static string GetGenericDefinition(IGeneric generic)
    {
      if (!generic.IsGeneric)
      {
        return "";
      }
      IEnumerable<NRTypeParameter> nrTypeParameters = generic.GenericTypes;
      StringBuilder result = new StringBuilder("<");
      foreach (NRTypeParameter nrTypeParameter in nrTypeParameters)
      {
        foreach(NRAttribute nrAttribute in nrTypeParameter.Attributes)
        {
          result.Append(GetAttribute(nrAttribute) + " ");
        }
        if(nrTypeParameter.IsIn)
        {
          result.Append("in ");
        }
        if(nrTypeParameter.IsOut)
        {
          result.Append("out ");
        }
        result.AppendFormat("{0}, ", nrTypeParameter.Name);
      }
      result.Length -= 2;
      result.Append(">");

      return result.ToString();
    }

    #endregion
  }
}