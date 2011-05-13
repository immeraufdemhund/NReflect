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
using System.Text;
using NReflect.Modifier;
using NReflect.NRMembers;
using NReflect.NRParameters;

namespace NReflect.NRCode
{
  /// <summary>
  /// Provides extension methods for some objects of NReflect to create C# code
  /// out of them.
  /// </summary>
  public static class CSharp
  {
    // ========================================================================
    // Methods

    #region === Methods

    /// <summary>
    /// Gets the <see cref="NREnumValue"/> as a C# string.
    /// </summary>
    /// <param name="enumValue">The enum value to get the code for.</param>
    /// <returns>A string representing the enum value.</returns>
    public static string Declaration(this NREnumValue enumValue)
    {
      string value = "";
      if (!String.IsNullOrWhiteSpace(enumValue.Value))
      {
        value = " = " + enumValue.Value;
      }

      return String.Format("{0}{1}", enumValue.Name, value);
    }

    /// <summary>
    /// Gets the <see cref="NREvent"/> as a C# string.
    /// </summary>
    /// <param name="nrEvent">The event to get the code for.</param>
    /// <returns>A string representing the event.</returns>
    public static string Declaration(this NREvent nrEvent)
    {
      string accessModifier = AddSpace(nrEvent.AccessModifier.Declaration());
      string modifier = AddSpace(nrEvent.OperationModifier.Declaration());

      return String.Format("{0}{1} event {2} {3}", accessModifier, modifier, nrEvent.Type, nrEvent.Name);
    }

    /// <summary>
    /// Gets the <see cref="NRField"/> as a C# string.
    /// </summary>
    /// <param name="field">The field to get the code for.</param>
    /// <returns>A string representing the field.</returns>
    public static string Declaration(this NRField field)
    {
      string accessModifier = AddSpace(field.AccessModifier.Declaration());
      string modifier = AddSpace(field.FieldModifier.Declaration());
      string value = "";
      if(!String.IsNullOrWhiteSpace(field.InitialValue))
      {
        value = " = " + field.InitialValue;
      }

      return String.Format("{0}{1}{2} {3}{4}", accessModifier, modifier, field.Type, field.Name, value);
    }

    /// <summary>
    /// Gets the <see cref="NRConstructor"/> as a C# string.
    /// </summary>
    /// <param name="constructor">The constructor to get the code for.</param>
    /// <returns>A string representing the constructor.</returns>
    public static string Declaration(this NRConstructor constructor)
    {
      string accessModifier = AddSpace(constructor.AccessModifier.Declaration());
      string modifier = AddSpace(constructor.OperationModifier.Declaration());

      return String.Format("{0}{1} {2}({3})", accessModifier, modifier, constructor.Name,
                           constructor.Parameters.Declaration());
    }

    /// <summary>
    /// Gets the <see cref="NRMethod"/> as a C# string.
    /// </summary>
    /// <param name="method">The method to get the code for.</param>
    /// <returns>A string representing the method.</returns>
    public static string Declaration(this NRMethod method)
    {
      string accessModifier = AddSpace(method.AccessModifier.Declaration());
      string modifier = AddSpace(method.OperationModifier.Declaration());

      return String.Format("{0}{1}{2} {3}({4})", accessModifier, modifier, method.Type, method.Name,
                           method.Parameters.Declaration());
    }

    /// <summary>
    /// Gets the <see cref="NROperator"/> as a C# string.
    /// </summary>
    /// <param name="nrOperator">The operator to get the code for.</param>
    /// <returns>A string representing the operator.</returns>
    public static string Declaration(this NROperator nrOperator)
    {
      string accessModifier = AddSpace(nrOperator.AccessModifier.Declaration());
      string modifier = AddSpace(nrOperator.OperationModifier.Declaration());
      string returnType = "";
      if(!nrOperator.Name.StartsWith("implicit") && !nrOperator.Name.StartsWith("explicit"))
      {
        returnType = nrOperator.Type + " ";
      }

      return String.Format("{0}{1}{2}{3}({4})", accessModifier, modifier, returnType, nrOperator.Name,
                           nrOperator.Parameters.Declaration());
    }

    /// <summary>
    /// Gets the <see cref="NRProperty"/> as a C# string.
    /// </summary>
    /// <param name="property">The property to get the code for.</param>
    /// <returns>A string representing the property.</returns>
    public static string Declaration(this NRProperty property)
    {
      string accessModifier = AddSpace(property.AccessModifier.Declaration());
      string modifier = AddSpace(property.OperationModifier.Declaration());
      string parameter = "";
      string getter = "";
      string setter = "";
      if(property.Parameters.Count > 0)
      {
        parameter = "[" + property.Parameters.Declaration() + "]";
      }
      if (property.HasGetter)
      {
        string getterModifier = "";
        if (property.GetterModifier > property.AccessModifier)
        {
          getterModifier = AddSpace(property.GetterModifier.Declaration());
        }
        getter = getterModifier + "get;";
      }
      if (property.HasSetter)
      {
        string setterModifier = "";
        if (property.SetterModifier > property.AccessModifier)
        {
          setterModifier = AddSpace(property.SetterModifier.Declaration());
        }
        setter = setterModifier + "set;";
      }

      return String.Format("{0}{1}{2} {3}{4}{{ {5}{6} }}", accessModifier, modifier, property.Type, property.Name, parameter,
                           getter, setter);
    }

    /// <summary>
    /// Gets a list of <see cref="NRParameter"/>s as a C# string.
    /// </summary>
    /// <param name="parameters">The parameters to get the code for.</param>
    /// <returns>A string representing the parameters.</returns>
    public static string Declaration(this IEnumerable<NRParameter> parameters)
    {
      StringBuilder result = new StringBuilder();
      foreach(NRParameter nrParameter in parameters)
      {
        result.Append(nrParameter.Declaration());
        result.Append(", ");
      }
      if(result.Length > 2)
      {
        result.Length -= 2;
      }

      return result.ToString();
    }

    /// <summary>
    /// Gets the <see cref="NRParameter"/> as a C# string.
    /// </summary>
    /// <param name="parameter">The parameter to get the code for.</param>
    /// <returns>A string representing the parameter.</returns>
    public static string Declaration(this NRParameter parameter)
    {
      string modifier = AddSpace(parameter.ParameterModifier.Declaration());
      string type = parameter.Type;
      if (type.EndsWith("&"))
      {
        type = type.Substring(0, type.Length - 1);
      }
      string defaultValue = "";
      if (!String.IsNullOrWhiteSpace(parameter.DefaultValue))
      {
        defaultValue = " = " + parameter.DefaultValue;
      }

      return String.Format("{0}{1} {2}{3}", modifier, type, parameter.Name,
                           defaultValue);
    }

    /// <summary>
    /// Gets the <see cref="OperationModifier"/> as a C# string.
    /// </summary>
    /// <param name="modifier">The <see cref="OperationModifier"/> to convert.</param>
    /// <returns>The <see cref="OperationModifier"/> as a string.</returns>
    public static string Declaration(this OperationModifier modifier)
    {
      if((modifier & OperationModifier.None) > 0)
      {
        return "";
      }
      StringBuilder result = new StringBuilder();
      if ((modifier & OperationModifier.Hider) != 0)
      {
        result.Append("new ");
      }
      if ((modifier & OperationModifier.Static) != 0)
      {
        result.Append("static ");
      }
      if ((modifier & OperationModifier.Virtual) != 0 && (modifier & OperationModifier.Override) == 0)
      {
        result.Append("virtual ");
      }
      if ((modifier & OperationModifier.Abstract) != 0)
      {
        result.Append("abstract ");
      }
      if ((modifier & OperationModifier.Sealed) != 0)
      {
        result.Append("sealed ");
      }
      if ((modifier & OperationModifier.Override) != 0)
      {
        result.Append("override ");
      }

      return result.ToString();
    }

    /// <summary>
    /// Gets the <see cref="AccessModifier"/> as a C# string.
    /// </summary>
    /// <param name="modifier">The <see cref="AccessModifier"/> to convert.</param>
    /// <returns>The <see cref="AccessModifier"/> as a string.</returns>
    public static string Declaration(this AccessModifier modifier)
    {
      switch (modifier)
      {
        case AccessModifier.Default:
          return "";
        case AccessModifier.Public:
          return "public";
        case AccessModifier.ProtectedInternal:
          return "protected internal";
        case AccessModifier.Internal:
          return "internal";
        case AccessModifier.Protected:
          return "protected";
        case AccessModifier.Private:
          return "private";
        default:
          return "";
      }
    }

    /// <summary>
    /// Gets the <see cref="ParameterModifier"/> as a C# string.
    /// </summary>
    /// <param name="modifier">The <see cref="ParameterModifier"/> to convert.</param>
    /// <returns>The <see cref="ParameterModifier"/> as a string.</returns>
    public static string Declaration(this ParameterModifier modifier)
    {
      switch (modifier)
      {
        case ParameterModifier.In:
          return "";
        case ParameterModifier.InOut:
          return "ref";
        case ParameterModifier.Out:
          return "out";
        case ParameterModifier.Params:
          return "params";
        case ParameterModifier.Optional:
          return "";
        default:
          return "";
      }
    }

    /// <summary>
    /// Gets the <see cref="FieldModifier"/> as a C# string.
    /// </summary>
    /// <param name="modifier">The <see cref="FieldModifier"/> to convert.</param>
    /// <returns>The <see cref="FieldModifier"/> as a string.</returns>
    public static string Declaration(this FieldModifier modifier)
    {
      switch (modifier)
      {
        case FieldModifier.Static:
          return "static";
        case FieldModifier.Readonly:
          return "readonly";
        case FieldModifier.Constant:
          return "const";
        case FieldModifier.Hider:
          return "new";
        case FieldModifier.Volatile:
          return "volatile";
        case FieldModifier.None:
          return "";
        default:
          return "";
      }
    }

    /// <summary>
    /// Returns a string containing the <paramref name="text"/> appended by a
    /// space if the text was not null or empty.
    /// </summary>
    /// <param name="text">The text to append the space to.</param>
    /// <returns>The text appended with a space if it is not null or empty.</returns>
    private static string AddSpace(string text)
    {
      if (!String.IsNullOrWhiteSpace(text))
      {
        return text + " ";
      }
      return text;
    }

    #endregion
  }
}