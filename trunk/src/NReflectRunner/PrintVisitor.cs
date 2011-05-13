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
using NReflect;
using NReflect.Modifier;
using NReflect.NREntities;
using NReflect.NRMembers;
using NReflect.NRParameters;

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

    private TextWriter writer;

    private int indent;

    #endregion

    // ========================================================================
    // Con- / Destruction

    #region === Con- / Destruction

    /// <summary>
    /// Initializes a new instance of <see cref="PrintVisitor"/>.
    /// </summary>
    public PrintVisitor()
    {
      writer = Console.Out;
      indent = 0;
    }

    #endregion

    // ========================================================================
    // Methods

    #region === Methods

    public void Visit(NRAssembly nrAssembly)
    {
      PrintEntities("delegates", nrAssembly.Delegates);
      PrintEntities("interfaces", nrAssembly.Interfaces);
      PrintEntities("structs", nrAssembly.Structs);
      PrintEntities("enums", nrAssembly.Enums);
      PrintEntities("classes", nrAssembly.Classes);
    }

    public void Visit(NRClass nrClass)
    {
      outputLine(ToString(nrClass.AccessModifier) + ToString(nrClass.ClassModifier) + "class " + nrClass.Name);
      outputLine("{");
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
      outputLine("}");
    }

    public void Visit(NRInterface nrInterface)
    {
      outputLine(ToString(nrInterface.AccessModifier) + "interface " + nrInterface.Name);
      outputLine("{");
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
      outputLine("}");
    }

    public void Visit(NRDelegate nrDelegate)
    {
      output(ToString(nrDelegate.AccessModifier) + "delegate " + nrDelegate.ReturnType + " " + nrDelegate.Name + "(");
      PrintParameters(nrDelegate.Parameters);
      outputLine(")", 0);
    }

    public void Visit(NRStruct nrStruct)
    {
      outputLine(ToString(nrStruct.AccessModifier) + "struct " + nrStruct.Name);
      outputLine("{");
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
      outputLine("}");
    }

    public void Visit(NREnum nrEnum)
    {
      outputLine(ToString(nrEnum.AccessModifier) + "enum " + nrEnum.Name);
      outputLine("{");
      indent++;
      foreach (NREnumValue nrEnumValue in nrEnum.Values)
      {
        nrEnumValue.Accept(this);
      }
      indent--;
      outputLine("}");
    }

    public void Visit(NRField nrField)
    {
      string value = "";
      if(nrField.IsConstant)
      {
        value = ": " + nrField.InitialValue;
      }
      outputLine(ToString(nrField.AccessModifier) + ToString(nrField.FieldModifier) + nrField.Type + " " + nrField.Name + value);
    }

    public void Visit(NRProperty nrProperty)
    {
      string methods = "";
      if(nrProperty.HasGetter)
      {
        methods = ToString(nrProperty.GetterModifier) + "get ";
      }
      if(nrProperty.HasSetter)
      {
        methods += ToString(nrProperty.SetterModifier) + "set ";
      }
      output(ToString(nrProperty.AccessModifier) + ToString(nrProperty.OperationModifier) + nrProperty.Type + " " +
             nrProperty.Name);
      if(nrProperty.Parameters.Count > 0)
      {
        output("[", 0);
        PrintParameters(nrProperty.Parameters);
        output("]", 0);
      }
      outputLine(" { " + methods + "}", 0);
    }

    public void Visit(NRMethod nrMethod)
    {
      output(ToString(nrMethod.AccessModifier) + ToString(nrMethod.OperationModifier) + nrMethod.Type + " " + nrMethod.Name + "(");
      PrintParameters(nrMethod.Parameters);
      outputLine(")", 0);
    }

    public void Visit(NROperator nrMethod)
    {
      string returnType = "";
      if (!String.IsNullOrWhiteSpace(nrMethod.Type))
      {
        returnType = nrMethod.Type + " ";
      }
      output(ToString(nrMethod.AccessModifier) + ToString(nrMethod.OperationModifier) + returnType + nrMethod.Name + "(");
      PrintParameters(nrMethod.Parameters);
      outputLine(")", 0);
    }

    public void Visit(NRConstructor nrConstructor)
    {
      output(ToString(nrConstructor.AccessModifier) + ToString(nrConstructor.OperationModifier) + nrConstructor.Name + "(");
      PrintParameters(nrConstructor.Parameters);
      outputLine(")", 0);
    }

    public void Visit(NREvent nrEvent)
    {
      output(ToString(nrEvent.AccessModifier) + "event " + nrEvent.Type + " " + nrEvent.Name + "(");
      PrintParameters(nrEvent.Parameters);
      outputLine(")", 0);
    }

    public void Visit(NRParameter nrParameter)
    {
      output(ToString(nrParameter.ParameterModifier) + nrParameter.Type + " " + nrParameter.Name, 0);
      if(nrParameter.ParameterModifier == ParameterModifier.Optional)
      {
        output(" = " + nrParameter.DefaultValue, 0);
      }
    }

    public void Visit(NREnumValue nrEnumValue)
    {
      string value = "";
      if(!String.IsNullOrWhiteSpace(nrEnumValue.Value))
      {
        value = " = " + nrEnumValue.Value;
      }
      outputLine(nrEnumValue.Name + value + ",");
    }

    /// <summary>
    /// Prints the entities.
    /// </summary>
    /// <param name="entityType">A string describing the type of the entities to print.</param>
    /// <param name="types">A list of entities to print.</param>
    private void PrintEntities(string entityType, IEnumerable<NRTypeBase> types)
    {
      outputLine(String.Format("Reflected {0}:", entityType));
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
    private void PrintParameters(List<NRParameter> nrParameters)
    {
      for(int i = 0; i < nrParameters.Count; i++)
      {
        nrParameters[i].Accept(this);
        if(i < nrParameters.Count - 1)
        {
          output(", ", 0);
        }
      }
    }

    private void output(string text)
    {
      output(text, indent);
    }

    private void output(string text, int indention)
    {
      writer.Write("{0," + indention * 2 + "}{1}", "", text);
    }

    private void outputLine(string text)
    {
      outputLine(text, indent);
    }

    private void outputLine(string text, int indention)
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

    #endregion
  }
}