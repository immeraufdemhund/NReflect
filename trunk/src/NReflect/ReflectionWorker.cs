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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using NReflect.Filter;
using NReflect.Modifier;
using NReflect.NREntities;
using NReflect.NRMembers;
using NReflect.NRParameters;
using ParameterModifier = NReflect.Modifier.ParameterModifier;

namespace NReflect
{
  /// <summary>
  /// This is the main class of the NReflect library.
  /// </summary>
  internal class ReflectionWorker
  {
    // ========================================================================
    // Constants

    #region === Constants

    /// <summary>
    /// Bindingflags which reflect every member.
    /// </summary>
    private const BindingFlags STANDARD_BINDING_FLAGS = BindingFlags.NonPublic |
                                                        BindingFlags.Public |
                                                        BindingFlags.Static |
                                                        BindingFlags.Instance;

    #endregion

    // ========================================================================
    // Fields

    #region === Fields

    /// <summary>
    /// The reflected assembly.
    /// </summary>
    private NRAssembly nrAssembly;

    /// <summary>
    /// The path of the assembly to import.
    /// </summary>
    private string path;

    /// <summary>
    /// Assemblies at the folder of the imported assembly. Only used if the imported
    /// assembly references an assembly which can't be loaded by the CLR.
    /// </summary>
    private Dictionary<String, Assembly> assemblies;

    /// <summary>
    /// Takes mappings from the full qualified name of a type to the generated NClass-
    /// entity.
    /// </summary>
    private readonly Dictionary<Type, NRTypeBase> entities;

    /// <summary>
    /// Mapping from operator-method to operator.
    /// </summary>
    private readonly Dictionary<string, string> operatorMethodsMap = new Dictionary<string, string>();

    #endregion

    // ========================================================================
    // Con- / Destruction

    #region === Con- / Destruction

    /// <summary>
    /// Initializes a new instance of <see cref="ReflectionWorker"/>.
    /// </summary>
    public ReflectionWorker()
    {
      entities = new Dictionary<Type, NRTypeBase>();
      Filter = new ReflectAllFilter();

      operatorMethodsMap.Add("op_UnaryPlus", "operator +");
      operatorMethodsMap.Add("op_UnaryNegation", "operator -");
      operatorMethodsMap.Add("op_LogicalNot", "operator !");
      operatorMethodsMap.Add("op_OnesComplement", "operator ~");
      operatorMethodsMap.Add("op_Increment", "operator ++");
      operatorMethodsMap.Add("op_Decrement", "operator --");
      operatorMethodsMap.Add("op_True", "operator true");
      operatorMethodsMap.Add("op_False", "operator false");
      operatorMethodsMap.Add("op_Addition", "operator +");
      operatorMethodsMap.Add("op_Subtraction", "operator -");
      operatorMethodsMap.Add("op_Multiply", "operator *");
      operatorMethodsMap.Add("op_Division", "operator /");
      operatorMethodsMap.Add("op_Modulus", "operator %");
      operatorMethodsMap.Add("op_BitwiseAnd", "operator &");
      operatorMethodsMap.Add("op_BitwiseOr", "operator |");
      operatorMethodsMap.Add("op_ExclusiveOr", "operator ^");
      operatorMethodsMap.Add("op_LeftShift", "operator <<");
      operatorMethodsMap.Add("op_RightShift", "operator >>");
      operatorMethodsMap.Add("op_Equality", "operator ==");
      operatorMethodsMap.Add("op_Inequality", "operator !=");
      operatorMethodsMap.Add("op_LessThan", "operator <");
      operatorMethodsMap.Add("op_GreaterThan", "operator >");
      operatorMethodsMap.Add("op_LessThanOrEqual", "operator <=");
      operatorMethodsMap.Add("op_GreaterThanOrEqual", "operator >=");
    }

    #endregion

    // ========================================================================
    // Properties

    #region === Properties

    /// <summary>
    /// Gets or sets the type filter used to determine which types and members to reflect.
    /// </summary>
    public IFilter Filter { get; set; }

    #endregion

    // ========================================================================
    // Methods

    #region === Methods

    /// <summary>
    /// Reflects the types of an assembly.
    /// </summary>
    /// <param name="fileName">The file name of the assembly to reflect.</param>
    /// <returns>The result of the reflection.</returns>
    public NRAssembly Reflect(string fileName)
    {
      nrAssembly = new NRAssembly();
      path = Path.GetDirectoryName(fileName);

      AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;
      Assembly assembly = Assembly.ReflectionOnlyLoadFrom(fileName);
      nrAssembly.FullName = assembly.FullName;
      nrAssembly.Source = fileName;

      ReflectTypes(assembly.GetTypes());

      return nrAssembly;
    }

    /// <summary>
    /// Find an assembly with the given full name. Called by the CLR if an assembly is
    /// loaded into the ReflectionOnlyContext and a referenced assebmly is needed.
    /// </summary>
    /// <param name="sender">The source of this event.</param>
    /// <param name="args">More information about the event.</param>
    /// <returns>The loaded assembly.</returns>
    private Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
    {
      try
      {
        return Assembly.ReflectionOnlyLoad(args.Name);
      }
      catch(FileNotFoundException)
      {
        //No global assembly: try loading it from the current dir.
        if(assemblies == null)
        {
          assemblies = new Dictionary<string, Assembly>();

          // Lazily load all assemblies from the path
          List<string> files = new List<string>();
          files.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly));
          files.AddRange(Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly));
          foreach(string file in files)
          {
            try
            {
              Assembly assembly = Assembly.ReflectionOnlyLoadFrom(file);
              assemblies.Add(assembly.FullName, assembly);
            }
            catch
            {
              // The assembly can't be loaded. Maybe this is no CLR assembly.
            }
          }
        }
        return assemblies.ContainsKey(args.Name) ? assemblies[args.Name] : null;
      }
    }

    #region +++ Reflect entities

    /// <summary>
    /// Reflect all given types and create NReflect-entities.
    /// </summary>
    /// <param name="types">An array of types to reflect.</param>
    private void ReflectTypes(IEnumerable<Type> types)
    {
      foreach(Type type in types)
      {
        //There are some compiler generated nested classes which should not
        //be reflected. All these magic classes have the CompilerGeneratedAttribute.
        //The C#-compiler of the .NET 2.0 Compact Framework creates the classes
        //but dosn't mark them with the attribute. All classes have a "<" in their name.
        if(HasMemberCompilerGeneratedAttribute(type) || type.Name.Contains("<"))
        {
          continue;
        }
        ReflectType(type);
      }
    }

    /// <summary>
    /// Reflect a given type and create the corresponding NReflect entity.
    /// </summary>
    /// <param name="type">The type to reflect.</param>
    private void ReflectType(Type type)
    {
      if(type == null || entities.ContainsKey(type))
      {
        // Type is already reflected - don't do it again.
        return;
      }

      if(type.IsClass)
      {
        //Could be a delegate
        if(type.BaseType == typeof(MulticastDelegate))
        {
          ReflectDelegate(type);
        }
        else
        {
          ReflectClass(type);
        }
      }
      if(type.IsInterface)
      {
        ReflectInterface(type);
      }
      if(type.IsEnum)
      {
        ReflectEnum(type);
      }
      if(type.IsValueType && !type.IsEnum)
      {
        ReflectStruct(type);
      }
    }

    /// <summary>
    /// Reflects the class <paramref name="type"/>.
    /// </summary>
    /// <param name="type">A type with informations about the class which gets reflected.</param>
    private void ReflectClass(Type type)
    {
      NRClass nrClass = new NRClass();

      ReflectEvents(type, nrClass);
      ReflectFields(type, nrClass);
      ReflectProperties(type, nrClass);
      ReflectConstructors(type, nrClass);
      ReflectMethods(type, nrClass);

      ReflectSingleInheritanceType(type, nrClass);
      ReflectTypeBase(type, nrClass);

      if(type.IsAbstract && type.IsSealed)
      {
        nrClass.ClassModifier = ClassModifier.Static;
      }
      else if(type.IsAbstract)
      {
        nrClass.ClassModifier = ClassModifier.Abstract;
      }
      else if(type.IsSealed)
      {
        nrClass.ClassModifier = ClassModifier.Sealed;
      }

      //Ask the filter if the class should be in the result.
      if(Filter.Reflect(nrClass))
      {
        nrAssembly.Classes.Add(nrClass);
      }
    }

    /// <summary>
    /// Reflects the delegate <paramref name="type"/>.
    /// </summary>
    /// <param name="type">A type with informations about the delegate which gets reflected.</param>
    private void ReflectDelegate(Type type)
    {
      MethodInfo methodInfo = type.GetMethod("Invoke");

      NRDelegate nrDelegate = new NRDelegate
                                {
                                  ReturnType = GetType(methodInfo.ReturnType, methodInfo)
                                };
      ReflectParameters(methodInfo, nrDelegate.Parameters);

      ReflectTypeBase(type, nrDelegate);

      //Ask the filter if the delegate should be in the result.
      if(Filter.Reflect(nrDelegate))
      {
        nrAssembly.Delegates.Add(nrDelegate);
      }
    }

    /// <summary>
    /// Reflects the interface <paramref name="type"/>.
    /// </summary>
    /// <param name="type">A type with informations about the interface which gets reflected.</param>
    private void ReflectInterface(Type type)
    {
      NRInterface nrInterface = new NRInterface();
      ReflectEvents(type, nrInterface);
      ReflectProperties(type, nrInterface);
      ReflectMethods(type, nrInterface);

      ReflectTypeBase(type, nrInterface);

      //Ask the filter if the interface should be int the result.
      if(Filter.Reflect(nrInterface))
      {
        nrAssembly.Interfaces.Add(nrInterface);
      }
    }

    /// <summary>
    /// Reflects the struct <paramref name="type"/>.
    /// </summary>
    /// <param name="type">A type with informations about the struct which gets reflected.</param>
    private void ReflectStruct(Type type)
    {
      NRStruct nrStruct = new NRStruct();
      ReflectEvents(type, nrStruct);
      ReflectFields(type, nrStruct);
      ReflectProperties(type, nrStruct);
      ReflectConstructors(type, nrStruct);
      ReflectMethods(type, nrStruct);

      ReflectSingleInheritanceType(type, nrStruct);
      ReflectTypeBase(type, nrStruct);

      //Ask the filter if the struct should be in the result.
      if(Filter.Reflect(nrStruct))
      {
        nrAssembly.Structs.Add(nrStruct);
      }
    }

    /// <summary>
    /// Reflects the enum <paramref name="type"/>.
    /// </summary>
    /// <param name="type">A type with informations about the enum which gets reflected.</param>
    private void ReflectEnum(Type type)
    {
      NREnum nrEnum = new NREnum();
      FieldInfo[] fields = type.GetFields(STANDARD_BINDING_FLAGS);
      foreach(FieldInfo field in fields)
      {
        //Sort this special field out
        if(field.Name == "value__")
        {
          continue;
        }
        NREnumValue nrEnumValue = new NREnumValue
                                    {
                                      Name = field.Name
                                    };
        object rawConstantValue = field.GetRawConstantValue();
        if(rawConstantValue != null)
        {
          nrEnumValue.Value = rawConstantValue.ToString();
        }

        // Ask the filter if the field should be reflected
        if(Filter.Reflect(nrEnumValue))
        {
          nrEnum.Values.Add(nrEnumValue);
        }
      }
      ReflectTypeBase(type, nrEnum);

      //Ask the filter if the enum should be in the result.
      if(Filter.Reflect(nrEnum))
      {
        nrAssembly.Enums.Add(nrEnum);
      }
    }

    /// <summary>
    /// Reflect the information of a single inheritance type like a class or a struct.
    /// </summary>
    /// <param name="type">The information is taken from <paramref name="type"/>.</param>
    /// <param name="nrSingleInheritanceType">All information is stored in this TypeBase.</param>
    private void ReflectSingleInheritanceType(Type type, NRSingleInheritanceType nrSingleInheritanceType)
    {
      if(type.BaseType != null)
      {
        nrSingleInheritanceType.BaseType = type.BaseType.FullName;
      }
      foreach(Type implementedInterface in type.GetInterfaces())
      {
        nrSingleInheritanceType.ImplementedInterfaces.Add(implementedInterface.FullName ?? implementedInterface.Name);
      }
    }

    /// <summary>
    /// Reflect the basic type <paramref name="type"/>. All information is
    /// stored in <paramref name="nrTypeBase"/>.
    /// </summary>
    /// <param name="type">The information is taken from <paramref name="type"/>.</param>
    /// <param name="nrTypeBase">All information is stored in this TypeBase.</param>
    private void ReflectTypeBase(Type type, NRTypeBase nrTypeBase)
    {
      nrTypeBase.Name = GetTypeName(type);
      nrTypeBase.FullName = type.FullName;
      //Might set the wrong access modifier for nested classes. Will be
      //corrected when adding the nesting relationships.
      nrTypeBase.AccessModifier = GetTypeAccessModifier(type);

      //Fill up the dictionaries
      if(type.FullName != null)
      {
        entities.Add(type, nrTypeBase);
      }
      if(type.IsNested && type.DeclaringType != null)
      {
        nrTypeBase.Parent = type.DeclaringType.FullName;
      }
    }

    #endregion

    #region +++ Reflect members

    /// <summary>
    /// Reflects all events within the type <paramref name="type"/>. Reflected
    /// events are added to <paramref name="nrCompositeType"/>.
    /// </summary>
    /// <param name="type">The events are taken from this type.</param>
    /// <param name="nrCompositeType">Reflected events are added to this FieldContainer.</param>
    private void ReflectEvents(Type type, NRCompositeType nrCompositeType)
    {
      EventInfo[] eventInfos = type.GetEvents(STANDARD_BINDING_FLAGS);
      foreach(EventInfo eventInfo in eventInfos)
      {
        //Don't reflect derived events.
        if(eventInfo.DeclaringType != type)
        {
          continue;
        }
        //The access modifier isn't stored at the event. So we have to take
        //that from the corresponding add_XXX (or perhaps remove_XXX) method.
        MethodInfo xAddMethodInfo = eventInfo.GetAddMethod(true);
        NREvent nrEvent = new NREvent
                            {
                              Name = eventInfo.Name,
                              Type = GetType(eventInfo.EventHandlerType, eventInfo),
                              TypeFullName = eventInfo.EventHandlerType.FullName ?? eventInfo.EventHandlerType.Name
                            };

        if(!(nrCompositeType is NRInterface))
        {
          nrEvent.AccessModifier = GetMethodAccessModifier(xAddMethodInfo);
          nrEvent.IsStatic = xAddMethodInfo.IsStatic;
        }

        //Ask the filter if the event should be in the result.
        if(Filter.Reflect(nrEvent))
        {
          nrCompositeType.Events.Add(nrEvent);
        }
      }
    }

    /// <summary>
    /// Reflects all fields within the type <paramref name="type"/>. Reflected
    /// fields are added to <paramref name="nrSingleInheritanceType"/>.
    /// </summary>
    /// <param name="type">The fields are taken from this type.</param>
    /// <param name="nrSingleInheritanceType">Reflected fields are added to this FieldContainer.</param>
    private void ReflectFields(Type type, NRSingleInheritanceType nrSingleInheritanceType)
    {
      List<string> events = GetEventNames(type);

      FieldInfo[] fieldInfos = type.GetFields(STANDARD_BINDING_FLAGS);
      foreach(FieldInfo fieldInfo in fieldInfos)
      {
        //Don't reflect fields belonging to events
        if(events.Contains(fieldInfo.Name))
        {
          continue;
        }
        //Don't display derived fields.
        if(fieldInfo.DeclaringType != type)
        {
          continue;
        }
        //Don't add compiler generated fields (thanks to Luca).
        if(HasMemberCompilerGeneratedAttribute(fieldInfo))
        {
          continue;
        }

        NRField nrField = new NRField
                            {
                              Name = fieldInfo.Name,
                              AccessModifier = GetFieldAccessModifier(fieldInfo),
                              IsReadonly = fieldInfo.IsInitOnly,
                              IsStatic = fieldInfo.IsStatic,
                              Type = GetType(fieldInfo.FieldType, fieldInfo),
                              TypeFullName = fieldInfo.FieldType.FullName ?? fieldInfo.FieldType.Name
                            };

        Type[] customModifiers = fieldInfo.GetRequiredCustomModifiers();
        if(customModifiers.Contains(typeof(IsVolatile)))
        {
          nrField.IsVolatile = true;
        }

        if(IsFieldOverwritten(type.BaseType, fieldInfo))
        {
          nrField.IsHider = true;
        }

        if(fieldInfo.IsLiteral)
        {
          object rawConstantValue = fieldInfo.GetRawConstantValue();
          if(rawConstantValue != null)
          {
            nrField.InitialValue = rawConstantValue.ToString();
          }
          nrField.IsStatic = false;
          nrField.IsConstant = true;
        }

        //Ask the filter if the field should be in the result.
        if(Filter.Reflect(nrField))
        {
          nrSingleInheritanceType.Fields.Add(nrField);
        }
      }
    }

    /// <summary>
    /// Reflects all constructors of <paramref name="type"/> to <paramref name="nrSingleInheritanceType"/>.
    /// </summary>
    /// <param name="type">The type to take the constructors from.</param>
    /// <param name="nrSingleInheritanceType">The destination of the reflection.</param>
    private void ReflectConstructors(Type type, NRSingleInheritanceType nrSingleInheritanceType)
    {
      ConstructorInfo[] constructors = type.GetConstructors(STANDARD_BINDING_FLAGS);
      string typeName = type.Name;
      if(typeName.Contains("`"))
      {
        typeName = typeName.Substring(0, typeName.IndexOf("`"));
      }
      foreach(ConstructorInfo constructorInfo in constructors)
      {
        NRConstructor nrConstructor = new NRConstructor
                                        {
                                          Name = typeName,
                                          AccessModifier = GetMethodAccessModifier(constructorInfo),
                                          OperationModifier = GetOperationModifier(constructorInfo)
                                        };
        ReflectParameters(constructorInfo, nrConstructor.Parameters);

        //Ask the filter if the constructor should be in the result.
        if(Filter.Reflect(nrConstructor))
        {
          nrSingleInheritanceType.Constructors.Add(nrConstructor);
        }
      }
    }

    /// <summary>
    /// Reflects all methods of <paramref name="type"/> to <paramref name="nrCompositeType"/>.
    /// </summary>
    /// <param name="type">The type to take the methods from.</param>
    /// <param name="nrCompositeType">The destination of the reflection.</param>
    private void ReflectMethods(Type type, NRCompositeType nrCompositeType)
    {
      MethodInfo[] methods = type.GetMethods(STANDARD_BINDING_FLAGS);
      foreach(MethodInfo methodInfo in methods)
      {
        //Don't display derived Methods.
        if(methodInfo.DeclaringType != type)
        {
          continue;
        }
        //There are sometimes some magic methods like '<.ctor>b__0'. Those
        //methods are generated by the compiler and shouldn't be reflected.
        //Those methods have an attribute CompilerGeneratedAttribute.
        if(HasMemberCompilerGeneratedAttribute(methodInfo))
        {
          continue;
        }

        //We store the method name here so it is much easier to take care about operators
        string methodName = methodInfo.Name;
        bool isOperator = false;
        if(methodInfo.IsSpecialName)
        {
          //SpecialName means that this method is an automaticaly generated
          //method. This can be get_XXX and set_XXX for properties or
          //add_XXX and remove_XXX for events. There are also special name
          //methods starting with op_ for operators.
          if(!methodInfo.Name.StartsWith("op_"))
          {
            continue;
          }
          //!method.Name starts with 'op_' and so it is an operator.
          isOperator = true;
          //We have to get the 'real' method name here.
          if(operatorMethodsMap.ContainsKey(methodName))
          {
            methodName = operatorMethodsMap[methodName];
          }

          if(methodName == "op_Implicit")
          {
            methodName = "implicit operator " + GetTypeName(methodInfo.ReturnType);
          }
          else if(methodName == "op_Explicit")
          {
            methodName = "explicit operator " + GetTypeName(methodInfo.ReturnType);
          }
        }

        NROperation nrOperation;
        if(isOperator)
        {
          nrOperation = new NROperator();
        }
        else
        {
          nrOperation = new NRMethod();
        }
        nrOperation.Name = methodName;
        nrOperation.Type = GetType(methodInfo.ReturnType, methodInfo);
        nrOperation.TypeFullName = methodInfo.ReturnType.FullName ?? methodInfo.ReturnType.Name;

        if(!(nrCompositeType is NRInterface))
        {
          nrOperation.AccessModifier = GetMethodAccessModifier(methodInfo);
          nrOperation.OperationModifier = GetOperationModifier(methodInfo);
        }

        ChangeOperationModifierIfOverwritten(type, methodInfo, nrOperation);

        ReflectParameters(methodInfo, nrOperation.Parameters);

        if(isOperator)
        {
          //Ask the filter if the method should be in the result.
          if(Filter.Reflect((NROperator)nrOperation) && nrCompositeType is NRSingleInheritanceType)
          {
            ((NRSingleInheritanceType)nrCompositeType).Operators.Add((NROperator)nrOperation);
          }
        }
        else
        {
          //Ask the filter if the method should be in the result.
          if(Filter.Reflect((NRMethod)nrOperation))
          {
            nrCompositeType.Methods.Add((NRMethod)nrOperation);
          }
        }
      }
    }

    /// <summary>
    /// Reflects all properties of <paramref name="type"/> to <paramref name="nrCompositeType"/>.
    /// </summary>
    /// <param name="type">The type to take the properties from.</param>
    /// <param name="nrCompositeType">The destination of the reflection.</param>
    private void ReflectProperties(Type type, NRCompositeType nrCompositeType)
    {
      PropertyInfo[] properties = type.GetProperties(STANDARD_BINDING_FLAGS);
      foreach(PropertyInfo propertyInfo in properties)
      {
        //Don't display derived Methods.
        if(propertyInfo.DeclaringType != type)
        {
          continue;
        }

        //The access modifier for a property isn't stored at the property.
        //We have to use the access modifier from the corresponding get_XXX /
        //set_XXX Method. The one whith the lowest restrictivity is the one
        //of the property.
        MethodInfo getMethod = propertyInfo.GetGetMethod(true);
        MethodInfo setMethod = propertyInfo.GetSetMethod(true);

        NRProperty nrProperty = new NRProperty();

        //The access modifier for the whole property is the most non destrictive one.
        if(!(nrCompositeType is NRInterface))
        {
          if(propertyInfo.CanRead && propertyInfo.CanWrite)
          {
            nrProperty.GetterModifier = GetMethodAccessModifier(getMethod);
            nrProperty.SetterModifier = GetMethodAccessModifier(setMethod);
            nrProperty.AccessModifier = nrProperty.GetterModifier > nrProperty.SetterModifier
                                          ? nrProperty.SetterModifier
                                          : nrProperty.GetterModifier;
            nrProperty.OperationModifier = GetOperationModifier(getMethod);
          }
          else if(propertyInfo.CanRead)
          {
            nrProperty.GetterModifier = GetMethodAccessModifier(getMethod);
            nrProperty.AccessModifier = nrProperty.GetterModifier;
            nrProperty.OperationModifier = GetOperationModifier(getMethod);
          }
          else
          {
            nrProperty.SetterModifier = GetMethodAccessModifier(setMethod);
            nrProperty.AccessModifier = nrProperty.SetterModifier;
            nrProperty.OperationModifier = GetOperationModifier(setMethod);
          }
        }
        if(!(nrCompositeType is NRInterface))
        {
          ChangeOperationModifierIfOverwritten(type, propertyInfo.CanRead ? getMethod : setMethod, nrProperty);
        }
        nrProperty.Type = GetType(propertyInfo.PropertyType, propertyInfo);
        nrProperty.TypeFullName = propertyInfo.PropertyType.FullName ?? propertyInfo.PropertyType.Name;
        //Is this an Item-property (public int this[int i])?
        if(propertyInfo.GetIndexParameters().Length > 0)
        {
          nrProperty.Name = "this";
          ReflectParameters(propertyInfo.CanRead ? getMethod : setMethod, nrProperty.Parameters);
        }
        else
        {
          nrProperty.Name = propertyInfo.Name;
        }

        nrProperty.HasGetter = propertyInfo.CanRead;
        nrProperty.HasSetter = propertyInfo.CanWrite;

        //Ask the filter if the property should be in the result.
        if(Filter.Reflect(nrProperty))
        {
          nrCompositeType.Properties.Add(nrProperty);
        }
      }
    }

    /// <summary>
    /// Adds all parameters of <paramref name="methodBase"/> to the list of <see cref="NRParameter"/>.
    /// </summary>
    /// <param name="methodBase">The paramters of this methods are extracted.</param>
    /// <param name="nrParameters">A list to add the parameters to.</param>
    private void ReflectParameters(MethodBase methodBase, List<NRParameter> nrParameters)
    {
      ParameterInfo[] parameters = methodBase.GetParameters();
      foreach(ParameterInfo parameter in parameters)
      {
        NRParameter nrParameter = new NRParameter
                                    {
                                      Name = parameter.Name,
                                      Type = GetType(parameter.ParameterType, parameter),
                                      TypeFullName = parameter.ParameterType.FullName ?? parameter.ParameterType.Name,
                                      ParameterModifier = ParameterModifier.In
                                    };
        if(parameter.ParameterType.Name.EndsWith("&"))
        {
          //This is a out or ref-parameter, otherwise it would not have the '&'
          nrParameter.ParameterModifier = parameter.IsOut ? ParameterModifier.Out : ParameterModifier.InOut;
        }
        else if(HasParamterAttribute(parameter, typeof(ParamArrayAttribute)))
        {
          nrParameter.ParameterModifier = ParameterModifier.Params;
        }
        else if(parameter.IsOptional)
        {
          nrParameter.ParameterModifier = ParameterModifier.Optional;
          object rawDefaultValue = parameter.RawDefaultValue;
          if(rawDefaultValue != null)
          {
            nrParameter.DefaultValue = rawDefaultValue.ToString();
          }
        }

        nrParameters.Add(nrParameter);
      }
    }

    #endregion

    #region === Help methods

    #region +++ bool Is...

    /// <summary>
    /// Tests recursiv if the <paramref name="memberInfo"/> or its declaring type
    /// has the CompilerGeneratedAttribute.
    /// </summary>
    /// <param name="memberInfo">The member info to test</param>
    /// <returns>True, if <paramref name="memberInfo"/> has the CompilerGeneratedAttribute.</returns>
    private static bool HasMemberCompilerGeneratedAttribute(MemberInfo memberInfo)
    {
      if(memberInfo == null)
      {
        return false;
      }
      if(HasMemberAttribute(memberInfo, typeof(CompilerGeneratedAttribute)))
      {
        return true;
      }
      return HasMemberCompilerGeneratedAttribute(memberInfo.DeclaringType);
    }

    /// <summary>
    /// Checks if the memberInfo contains an attribute of the given type.
    /// </summary>
    /// <param name="memberInfo">The MemberInfo</param>
    /// <param name="type">The type of the attribute.</param>
    /// <returns>True if the memeber contains the attribute, false otherwise.</returns>
    private static bool HasMemberAttribute(MemberInfo memberInfo, Type type)
    {
      if(memberInfo == null)
      {
        return false;
      }
      IList<CustomAttributeData> attributeDatas = CustomAttributeData.GetCustomAttributes(memberInfo);
      return attributeDatas.Any(attributeData => attributeData.Constructor.DeclaringType == type);
    }

    /// <summary>
    /// Checks if the parameterInfo contains an attribute of the given type.
    /// </summary>
    /// <param name="parameterInfo">The ParameterInfo</param>
    /// <param name="type">The type of the attribute.</param>
    /// <returns>True if the parameter contains the attribute, false otherwise.</returns>
    private static bool HasParamterAttribute(ParameterInfo parameterInfo, Type type)
    {
      if(parameterInfo == null)
      {
        return false;
      }
      IList<CustomAttributeData> attributeDatas = CustomAttributeData.GetCustomAttributes(parameterInfo);
      return attributeDatas.Any(attributeData => attributeData.Constructor.DeclaringType == type);
    }

    /// <summary>
    /// Determines if the method <paramref name="method"/> is already
    /// defined within <paramref name="type"/> or above.
    /// </summary>
    /// <param name="type">The type which could define <paramref name="method"/> already.</param>
    /// <param name="method">The method wich should be checked</param>
    /// <returns>True, if <paramref name="method"/> is defined in <paramref name="type"/> or above.</returns>
    private static bool IsMethodOverwritten(Type type, MethodBase method)
    {
      if(type == null)
      {
        return false;
      }
      ParameterInfo[] parameters = method.GetParameters();
      Type[] parameterTypes = new Type[parameters.Length];
      for(int i = 0; i < parameters.Length; i++)
      {
        parameterTypes[i] = parameters[i].ParameterType;
      }
      IEnumerable<MethodInfo> methodInfos = type.GetMethods(method.Name, parameterTypes);
      if(methodInfos != null && methodInfos.Count() > 0)
      {
        return true;
      }
      return IsMethodOverwritten(type.BaseType, method);
    }

    /// <summary>
    /// Determines if the field <paramref name="fieldInfo"/> is already
    /// defined within <paramref name="type"/> or above.
    /// </summary>
    /// <param name="type">The type which could define <paramref name="fieldInfo"/> already.</param>
    /// <param name="fieldInfo">The field wich will be checked.</param>
    /// <returns><c>True</c> if <paramref name="fieldInfo"/> is defined in <paramref name="type"/> or above.</returns>
    private static bool IsFieldOverwritten(Type type, FieldInfo fieldInfo)
    {
      if(type == null)
      {
        return false;
      }
      FieldInfo parentField = type.GetField(fieldInfo.Name, STANDARD_BINDING_FLAGS);
      if(parentField != null)
      {
        return true;
      }
      return IsFieldOverwritten(type.BaseType, fieldInfo);
    }

    #endregion

    #region +++ GetType

    /// <summary>
    /// Returns an instance of <see cref="NRType"/> which is initialized with the
    /// values for the given type.
    /// </summary>
    /// <param name="type">The type which will be represented by the resulting <see cref="NRType"/>.</param>
    /// <param name="memberInfo">A <see cref="MemberInfo"/> which is used to determine if the type is dynamic.</param>
    /// <returns>The initialized <see cref="NRType"/>.</returns>
    private static NRType GetType(Type type, MemberInfo memberInfo)
    {
      return GetType(type, CustomAttributeData.GetCustomAttributes(memberInfo));
    }

    /// <summary>
    /// Returns an instance of <see cref="NRType"/> which is initialized with the
    /// values for the given type.
    /// </summary>
    /// <param name="type">The type which will be represented by the resulting <see cref="NRType"/>.</param>
    /// <param name="methodInfo">A <see cref="MethodInfo"/> which is used to determine if the type is dynamic.</param>
    /// <returns>The initialized <see cref="NRType"/>.</returns>
    private static NRType GetType(Type type, MethodInfo methodInfo)
    {
      return GetType(type, (ParameterInfo)methodInfo.ReturnTypeCustomAttributes);
    }

    /// <summary>
    /// Returns an instance of <see cref="NRType"/> which is initialized with the
    /// values for the given type.
    /// </summary>
    /// <param name="type">The type which will be represented by the resulting <see cref="NRType"/>.</param>
    /// <param name="parameterInfo">A <see cref="ParameterInfo"/> which is used to determine if the type is dynamic.</param>
    /// <returns>The initialized <see cref="NRType"/>.</returns>
    private static NRType GetType(Type type, ParameterInfo parameterInfo)
    {
      return GetType(type, CustomAttributeData.GetCustomAttributes(parameterInfo));
    }
    
    /// <summary>
    /// Returns an instance of <see cref="NRType"/> which is initialized with the
    /// values for the given type.
    /// </summary>
    /// <param name="type">The type which will be represented by the resulting <see cref="NRType"/>.</param>
    /// <param name="customAttributeDatas">The custom attributes of the type which are used to determine if the type is dynamic.</param>
    /// <returns>The initialized <see cref="NRType"/>.</returns>
    private static NRType GetType(Type type, IEnumerable<CustomAttributeData> customAttributeDatas)
    {
      return new NRType
               {
                 Type = GetTypeName(type),
                 IsDynamic =
                   customAttributeDatas.Any(
                                            ad =>
                                            ad.Constructor != null && ad.Constructor.DeclaringType.FullName != null &&
                                            ad.Constructor.DeclaringType.FullName.Equals(
                                                                                         typeof(DynamicAttribute).
                                                                                           FullName))
               };
    }

    #endregion

    /// <summary>
    /// Returns a string containing the name of the type <paramref name="type"/>
    /// in C# syntax. Is especially responsible to solve problems with generic
    /// types.
    /// </summary>
    /// <param name="type">The type name is returned for this <see cref="Type"/>.</param>
    /// <returns>The name of <paramref name="type"/> as a string.</returns>
    private static string GetTypeName(Type type)
    {
      StringBuilder typeName = new StringBuilder(type.Name);
      if(type.IsArray)
      {
        typeName =
          new StringBuilder(GetTypeName(type.GetElementType()) + type.Name.Substring(type.GetElementType().Name.Length));
      }
      else if(type.IsGenericType)
      {
        if(typeName.ToString().LastIndexOf('`') > 0)
        {
          //Generics get names like "List`1"
          typeName.Remove(typeName.ToString().LastIndexOf('`'),
                          typeName.Length - typeName.ToString().LastIndexOf('`'));
        }
        Type[] genericArguments = type.GetGenericArguments();
        typeName.Append("<");
        int parentGenericArgsCount = 0;
        if(type.DeclaringType != null)
        {
          // This is a nested type. Be sure to check the generic type arguments.
          parentGenericArgsCount = type.DeclaringType.GetGenericArguments().Length;
        }
        foreach(Type genericArgument in genericArguments.Skip(parentGenericArgsCount))
        {
          typeName.AppendFormat("{0}, ", GetTypeName(genericArgument));
        }
        //Get rid of ", " one time
        typeName.Length -= 2;
        typeName.Append(">");

        //Could be a nullable type. We should cast it to the ? form (int? instead of Nullable<int>)
        if(type.FullName != null && type.FullName.StartsWith("System.Nullable"))
        {
          string typeString = typeName.ToString();
          typeName = new StringBuilder(typeString.Substring(typeString.IndexOf('<') + 1));
          typeName.Length -= 1;
          typeName.Append('?');
        }
      }
      //openvenom - This part is especially added to handle ref Nullable<T> (ex: ref int32?)
      //kind method parameter types
      //To Malte Ried: Thank you very much for providing such a nice utility...
      else if (!type.IsGenericType && type.IsByRef && type.GetGenericArguments().Length > 0
               && type.FullName != null && type.FullName.StartsWith("System.Nullable`1") 
               && type.FullName.EndsWith("&"))
      {
          typeName = new StringBuilder();
          typeName.Append(type.GetGenericArguments()[0].Name); //This gives us the Int32 part
          typeName.Append("?");
      }
      return typeName.ToString();
    }

    /// <summary>
    /// Gets a list of the names of all events declared by the given type.
    /// </summary>
    /// <param name="type">The event names are extrected from this type.</param>
    /// <returns>A list of event names.</returns>
    private static List<string> GetEventNames(Type type)
    {
      EventInfo[] eventInfos = type.GetEvents(STANDARD_BINDING_FLAGS);
      List<string> eventNames = new List<string>(from eventInfo in eventInfos
                                                 where eventInfo.DeclaringType == type
                                                 select eventInfo.Name);
      return eventNames;
    }

    /// <summary>
    /// If the method <paramref name="method"/> is overwritten in type
    /// <paramref name="type"/> the operation modifiers are changed to 
    /// reflect this.
    /// </summary>
    /// <param name="type">The type the method is declared in.</param>
    /// <param name="method">The method to check.</param>
    /// <param name="nrOperation">The operation which has to be changed.</param>
    private static void ChangeOperationModifierIfOverwritten(Type type, MethodBase method, NROperation nrOperation)
    {
      if(IsMethodOverwritten(type.BaseType, method))
      {
        if(method.IsVirtual &&
           (method.Attributes & MethodAttributes.VtableLayoutMask) != MethodAttributes.VtableLayoutMask)
        {
          if(method.IsFinal)
          {
            nrOperation.IsSealed = true;
          }
          nrOperation.IsOverride = true;
        }
          //It's not possible to distinguish between virtual and virtual new
          //in the assembly, because virtual methods get implicitly virtual new.
        else
        {
          nrOperation.IsHider = true;
        }
      }
    }

    #region +++ Get modifiers

    /// <summary>
    /// Returns the operation modifier for <paramref name="method"/>.
    /// </summary>
    /// <param name="method">The operation modifiers is returned for this MethodBase.</param>
    /// <returns>The OperationModifier of <paramref name="method"/>.</returns>
    private static OperationModifier GetOperationModifier(MethodBase method)
    {
      if(method.DeclaringType.IsValueType)
      {
        return OperationModifier.None;
      }

      OperationModifier result = OperationModifier.None;
      if(method.IsStatic)
      {
        result |= OperationModifier.Static;
      }
      if(method.IsAbstract)
      {
        result |= OperationModifier.Abstract;
      }
      // lytico: possible value is: IsFinal AND IsVirtual
      if(method.IsFinal && method.IsVirtual)
      {
        return OperationModifier.None;
      }
      if(method.IsFinal)
      {
        result |= OperationModifier.Sealed;
      }
      if(!method.IsAbstract && method.IsVirtual)
      {
        result |= OperationModifier.Virtual;
      }
      return result;
    }

    /// <summary>
    /// Returns the access modifier for the type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The access modifier is returned for this Type.</param>
    /// <returns>The AccessModifier of <paramref name="type"/>.</returns>
    private static AccessModifier GetTypeAccessModifier(Type type)
    {
      if(type.IsNested)
      {
        if(type.IsNestedPublic)
        {
          return AccessModifier.Public;
        }
        if(type.IsNestedPrivate)
        {
          return AccessModifier.Private;
        }
        if(type.IsNestedAssembly)
        {
          return AccessModifier.Internal;
        }
        if(type.IsNestedFamily)
        {
          return AccessModifier.Protected;
        }
        if(type.IsNestedFamORAssem)
        {
          return AccessModifier.ProtectedInternal;
        }
        return AccessModifier.Default;
      }
      if(type.IsPublic)
      {
        return AccessModifier.Public;
      }
      if(type.IsNotPublic)
      {
        return AccessModifier.Internal;
      }
      if(!type.IsVisible)
      {
        return AccessModifier.Internal;
      }
      return AccessModifier.Default;
    }

    /// <summary>
    /// Returns the access modifier for the field <paramref name="field"/>.
    /// </summary>
    /// <param name="field">The access modifier is returned for this FieldInfo.</param>
    /// <returns>The AccessModifier of <paramref name="field"/>.</returns>
    private static AccessModifier GetFieldAccessModifier(FieldInfo field)
    {
      if(field.IsPublic)
      {
        return AccessModifier.Public;
      }
      if(field.IsPrivate)
      {
        return AccessModifier.Private;
      }
      if(field.IsAssembly)
      {
        return AccessModifier.Internal;
      }
      if(field.IsFamily)
      {
        return AccessModifier.Protected;
      }
      if(field.IsFamilyOrAssembly)
      {
        return AccessModifier.ProtectedInternal;
      }
      return AccessModifier.Default;
    }

    /// <summary>
    /// Returns the access modifier for the method <paramref name="methodBase"/>.
    /// </summary>
    /// <param name="methodBase">The access modifier is returned for this MethodBase.</param>
    /// <returns>The AccessModifier of <paramref name="methodBase"/>.</returns>
    private static AccessModifier GetMethodAccessModifier(MethodBase methodBase)
    {
      if(methodBase.Name.Contains(".") && !methodBase.IsConstructor)
      {
        //explicit interface implementation
        return AccessModifier.Default;
      }
      if(methodBase.IsPublic)
      {
        return AccessModifier.Public;
      }
      if(methodBase.IsPrivate)
      {
        return AccessModifier.Private;
      }
      if(methodBase.IsAssembly)
      {
        return AccessModifier.Internal;
      }
      if(methodBase.IsFamily)
      {
        return AccessModifier.Protected;
      }
      if(methodBase.IsFamilyOrAssembly)
      {
        return AccessModifier.ProtectedInternal;
      }
      return AccessModifier.Default;
    }

    #endregion

    #endregion

    #endregion
  }
}