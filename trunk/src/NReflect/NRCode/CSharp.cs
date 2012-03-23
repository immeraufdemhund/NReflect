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
using NReflect.NRAttributes;
using NReflect.NREntities;
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
    /// Gets the <see cref="NRClass"/> as a C# string. Only the head of the class
    /// is returned.
    /// </summary>
    /// <param name="nrClass">The class to get the code for.</param>
    /// <returns>A string representing the class header.</returns>
    public static string Declaration(this NRClass nrClass)
    {
      string accessModifier = AddSpace(nrClass.AccessModifier.Declaration());
      string modifier = AddSpace(nrClass.ClassModifier.Declaration());
      string genericDecl = GetGenericDefinition(nrClass);
      string baseDecl = "";
      if (nrClass.ClassModifier != ClassModifier.Static)
      {
        baseDecl = GetBaseTypeAndInterfaces(nrClass);
      }

      return String.Format("{0}{1}class {2}{3}{4}", accessModifier, modifier, nrClass.Name, genericDecl,
                           baseDecl);
    }

    /// <summary>
    /// Gets the <see cref="NRStruct"/> as a C# string. Only the head of the struct
    /// is returned.
    /// </summary>
    /// <param name="nrStruct">The struct to get the code for.</param>
    /// <returns>A string representing the struct header.</returns>
    public static string Declaration(this NRStruct nrStruct)
    {
      string accessModifier = AddSpace(nrStruct.AccessModifier.Declaration());
      string genericDecl = GetGenericDefinition(nrStruct);
      string baseDecl = GetImplementedInterfaces(nrStruct);

      return String.Format("{0}struct {1}{2}{3}", accessModifier, nrStruct.Name, genericDecl,
                           baseDecl);
    }

    /// <summary>
    /// Gets the <see cref="NRInterface"/> as a C# string. Only the head of the interface
    /// is returned.
    /// </summary>
    /// <param name="nrInterface">The interface to get the code for.</param>
    /// <returns>A string representing the interface header.</returns>
    public static string Declaration(this NRInterface nrInterface)
    {
      string accessModifier = AddSpace(nrInterface.AccessModifier.Declaration());
      string genericDecl = GetGenericDefinition(nrInterface);
      string baseDecl = GetImplementedInterfaces(nrInterface);

      return String.Format("{0}interface {1}{2}{3}", accessModifier, nrInterface.Name, genericDecl,
                           baseDecl);
    }

    /// <summary>
    /// Gets the <see cref="NRDelegate"/> as a C# string.
    /// </summary>
    /// <param name="nrDelegate">The delegate to get the code for.</param>
    /// <returns>A string representing the delegate.</returns>
    public static string Declaration(this NRDelegate nrDelegate)
    {
      string accessModifier = AddSpace(nrDelegate.AccessModifier.Declaration());
      string genericDecl = GetGenericDefinition(nrDelegate);

      return String.Format("{0}delegate {1} {2}{3}({4})", accessModifier, nrDelegate.ReturnType.Type, nrDelegate.Name, genericDecl,
                           nrDelegate.Parameters.Declaration());
    }

    /// <summary>
    /// Gets the <see cref="NREnum"/> as a C# string.
    /// </summary>
    /// <param name="nrEnum">The enum to get the code for.</param>
    /// <returns>A string representing the enum header.</returns>
    public static string Declaration(this NREnum nrEnum)
    {
      string accessModifier = AddSpace(nrEnum.AccessModifier.Declaration());
      string baseDecl = string.IsNullOrEmpty(nrEnum.Parent) ? "" : " : " + nrEnum.Parent;

      return String.Format("{0}enum {1}{2}", accessModifier, nrEnum.Name, baseDecl);
    }

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

      return String.Format("{0}{1}event {2} {3}", accessModifier, modifier, nrEvent.Type.Declaration(), nrEvent.Name);
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
      if(!String.IsNullOrWhiteSpace(field.InitialValue) || field.IsConstant)
      {
        value = " = " + (field.InitialValue ?? "null");
      }

      return String.Format("{0}{1}{2} {3}{4}", accessModifier, modifier, field.Type.Declaration(), field.Name, value);
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

      return String.Format("{0}{1}{2}({3})", accessModifier, modifier, constructor.Name,
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
      string genericDecl = GetGenericDefinition(method);

      return String.Format("{0}{1}{2} {3}{4}({5})", accessModifier, modifier, method.Type.Declaration(), method.Name,
                           genericDecl, method.Parameters.Declaration());
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
        returnType = nrOperator.Type.Declaration() + " ";
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

      return String.Format("{0}{1}{2} {3}{4}{{ {5}{6} }}", accessModifier, modifier, property.Type.Declaration(), property.Name, parameter,
                           getter, setter);
    }

    /// <summary>
    /// Gets the <see cref="NRType"/> as a C# string.
    /// </summary>
    /// <param name="type">The type to get the code for.</param>
    /// <returns>A string representing the type.</returns>
    public static string Declaration(this NRType type)
    {
      return type.IsDynamic ? "dynamic" : type.Type;
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
      string type = parameter.Type.Declaration();
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
    /// Gets the <see cref="ClassModifier"/> as a C# string.
    /// </summary>
    /// <param name="modifier">The <see cref="ClassModifier"/> to convert.</param>
    /// <returns>The <see cref="ClassModifier"/> as a string.</returns>
    public static string Declaration(this ClassModifier modifier)
    {
      switch (modifier)
      {
        case ClassModifier.None:
          return "";
        case ClassModifier.Abstract:
          return "abstract";
        case ClassModifier.Sealed:
          return "sealed";
        case ClassModifier.Static:
          return "static";
        default:
          return "";
      }
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
    /// Gets the <see cref="NRAttribute"/> as a C# string.
    /// </summary>
    /// <param name="attribute">The <see cref="NRAttribute"/> to convert.</param>
    /// <returns>The <see cref="NRAttribute"/> as a string.</returns>
    public static string Declaration(this NRAttribute attribute)
    {
      return GetAttribute(attribute);
    }

    /// <summary>
    /// Gets the <see cref="NRTypeParameter"/> as a C# string.
    /// </summary>
    /// <param name="nrTypeParameter">The <see cref="NRTypeParameter"/> to convert.</param>
    /// <returns>The <see cref="NRTypeParameter"/> as a string.</returns>
    public static string Declaration(this NRTypeParameter nrTypeParameter)
    {
      if (!nrTypeParameter.IsStruct && !nrTypeParameter.IsClass && nrTypeParameter.BaseTypes.Count <= 0 && !nrTypeParameter.IsConstructor && !nrTypeParameter.IsIn && !nrTypeParameter.IsOut)
      {
        return "";
      }

      StringBuilder result = new StringBuilder(" where " + nrTypeParameter.Name + " : ");
      if (nrTypeParameter.IsStruct)
      {
        result.Append("struct, ");
      }
      else if (nrTypeParameter.IsClass)
      {
        result.Append("class, ");
      }
      foreach (string baseType in nrTypeParameter.BaseTypes)
      {
        result.Append(baseType + ", ");
      }
      if (nrTypeParameter.IsConstructor)
      {
        result.Append("new(), ");
      }
      result.Length -= 2;

      return result.ToString();
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

    /// <summary>
    /// Gets a string representing the C#-Code for an attribute.
    /// </summary>
    /// <param name="nrAttribute">The attribute to show.</param>
    /// <param name="returnAttribute">Set to true if the attribute is taken from a return value.</param>
    /// <returns>A string representing the C#-Code for an attribute.</returns>
    private static string GetAttribute(NRAttribute nrAttribute, bool returnAttribute = false)
    {
      StringBuilder result = new StringBuilder("[");
      if (returnAttribute)
      {
        result.Append("return: ");
      }
      result.Append(nrAttribute.Name.EndsWith("Attribute")
                      ? nrAttribute.Name.Substring(0, nrAttribute.Name.Length - "Attribute".Length)
                      : nrAttribute.Name);
      if (nrAttribute.Values.Count > 0 || nrAttribute.NamedValues.Count > 0)
      {
        result.Append("(");
        foreach (NRAttributeValue value in nrAttribute.Values)
        {
          result.Append(GetAttributeValueString(value) + ", ");
        }
        foreach (string key in nrAttribute.NamedValues.Keys)
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
      if (value.Type == "System.String")
      {
        return "\"" + value.Value + "\"";
      }
      if (value.Type == "System.Type")
      {
        return "typeof(" + value.Value + ")";
      }
      Type type = Type.GetType(value.Type, false);
      if (type != null && type.IsEnum)
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
        catch (Exception)
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
        foreach (NRAttribute nrAttribute in nrTypeParameter.Attributes)
        {
          result.Append(GetAttribute(nrAttribute) + " ");
        }
        if (nrTypeParameter.IsIn)
        {
          result.Append("in ");
        }
        if (nrTypeParameter.IsOut)
        {
          result.Append("out ");
        }
        result.AppendFormat("{0}, ", nrTypeParameter.Name);
      }
      result.Length -= 2;
      result.Append(">");

      return result.ToString();
    }

    /// <summary>
    /// Gets the base type and all implemented interfaces of the given <see cref="NRSingleInheritanceType"/>.
    /// </summary>
    /// <param name="nrSingleInheritanceType">An <see cref="NRSingleInheritanceType"/> to take the base type and interfaces from.</param>
    private static string GetBaseTypeAndInterfaces(NRSingleInheritanceType nrSingleInheritanceType)
    {
      if (nrSingleInheritanceType.BaseType == null && nrSingleInheritanceType.ImplementedInterfaces.Count == 0)
      {
        return "";
      }
      StringBuilder result = new StringBuilder(" : ");
      if (nrSingleInheritanceType.BaseType != null)
      {
        result.Append(nrSingleInheritanceType.BaseType + ", ");
      }
      foreach (string implementedInterface in nrSingleInheritanceType.ImplementedInterfaces)
      {
        result.Append(implementedInterface + ", ");
      }
      result.Length -= 2;

      return result.ToString();
    }

    /// <summary>
    /// Gets all implemented interfaces of the given <see cref="NRCompositeType"/>.
    /// </summary>
    /// <param name="nrCompositeType">An <see cref="NRCompositeType"/> to take the interfaces from.</param>
    private static string GetImplementedInterfaces(NRCompositeType nrCompositeType)
    {
      if (nrCompositeType.ImplementedInterfaces.Count == 0)
      {
        return "";
      }
      StringBuilder result = new StringBuilder(" : ");
      foreach (string implementedInterface in nrCompositeType.ImplementedInterfaces)
      {
        result.Append(implementedInterface + ", ");
      }
      result.Length -= 2;

      return result.ToString();
    }

    #endregion
  }
}