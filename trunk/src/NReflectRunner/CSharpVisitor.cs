// NReflect - Easy assembly reflection
// Copyright (C) 2010-2012 Malte Ried
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
using System.IO;
using NReflect;
using NReflect.NRAttributes;
using NReflect.NREntities;
using NReflect.NRMembers;
using NReflect.NRParameters;
using NReflect.NRCode;

namespace NReflectRunner
{
  public class CSharpVisitor : VisitorBase, IVisitor
  {
    // ========================================================================
    // Con- / Destruction

    #region === Con- / Destruction

    /// <summary>
    /// Initializes a new instance of <see cref="PrintVisitor"/>.
    /// </summary>
    public CSharpVisitor()
      : base(Console.Out)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CSharpVisitor"/>.
    /// </summary>
    /// <param name="writer">This <see cref="TextWriter"/> will be used for output.</param>
    public CSharpVisitor(TextWriter writer) 
      : base(writer)
    {
    }

    #endregion

    // ========================================================================
    // Methods

    #region === Methods

    /// <summary>
    /// Visit a <see cref="NRAssembly"/>.
    /// </summary>
    /// <param name="nrAssembly">The <see cref="NRAssembly"/> to visit.</param>
    public void Visit(NRAssembly nrAssembly)
    {
      foreach (NRClass nrClass in nrAssembly.Classes)
      {
        nrClass.Accept(this);
      }
      foreach (NRStruct nrStruct in nrAssembly.Structs)
      {
        nrStruct.Accept(this);
      }
      foreach (NRInterface nrInterface in nrAssembly.Interfaces)
      {
        nrInterface.Accept(this);
      }
      foreach (NRDelegate nrDelegate in nrAssembly.Delegates)
      {
        nrDelegate.Accept(this);
      }
      foreach (NREnum nrEnum in nrAssembly.Enums)
      {
        nrEnum.Accept(this);
      }
    }

    /// <summary>
    /// Visit a <see cref="NRClass"/>.
    /// </summary>
    /// <param name="nrClass">The <see cref="NRClass"/> to visit.</param>
    public void Visit(NRClass nrClass)
    {
      OutputLine(nrClass.Declaration());
      OutputLine("{");
      indent++;
      foreach (NRField nrField in nrClass.Fields)
      {
        nrField.Accept(this);
      }
      foreach (NRConstructor nrConstructor in nrClass.Constructors)
      {
        nrConstructor.Accept(this);
      }
      foreach (NRProperty nrProperty in nrClass.Properties)
      {
        nrProperty.Accept(this);
      }
      foreach (NREvent nrEvent in nrClass.Events)
      {
        nrEvent.Accept(this);
      }
      foreach (NRMethod nrMethod in nrClass.Methods)
      {
        nrMethod.Accept(this);
      }
      foreach (NRTypeBase nrTypeBase in nrClass.NestedTypes)
      {
        nrTypeBase.Accept(this);
      }
      indent--;
      OutputLine("}");
    }

    /// <summary>
    /// Visit a <see cref="NRInterface"/>.
    /// </summary>
    /// <param name="nrInterface">The <see cref="NRInterface"/> to visit.</param>
    public void Visit(NRInterface nrInterface)
    {
      OutputLine(nrInterface.Declaration());
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
      foreach (NRMethod nrMethod in nrInterface.Methods)
      {
        nrMethod.Accept(this);
      }
      indent--;
      OutputLine("}");
    }

    /// <summary>
    /// Visit a <see cref="NRDelegate"/>.
    /// </summary>
    /// <param name="nrDelegate">The <see cref="NRDelegate"/> to visit.</param>
    public void Visit(NRDelegate nrDelegate)
    {
      OutputLine(nrDelegate.Declaration() + ";");
    }

    /// <summary>
    /// Visit a <see cref="NRStruct"/>.
    /// </summary>
    /// <param name="nrStruct">The <see cref="NRStruct"/> to visit.</param>
    public void Visit(NRStruct nrStruct)
    {
      OutputLine(nrStruct.Declaration());
      OutputLine("{");
      indent++;
      foreach (NRField nrField in nrStruct.Fields)
      {
        nrField.Accept(this);
      }
      foreach (NRConstructor nrConstructor in nrStruct.Constructors)
      {
        nrConstructor.Accept(this);
      }
      foreach (NRProperty nrProperty in nrStruct.Properties)
      {
        nrProperty.Accept(this);
      }
      foreach (NREvent nrEvent in nrStruct.Events)
      {
        nrEvent.Accept(this);
      }
      foreach (NRMethod nrMethod in nrStruct.Methods)
      {
        nrMethod.Accept(this);
      }
      foreach (NRTypeBase nrTypeBase in nrStruct.NestedTypes)
      {
        nrTypeBase.Accept(this);
      }
      indent--;
      OutputLine("}");
    }

    /// <summary>
    /// Visit a <see cref="NREnum"/>.
    /// </summary>
    /// <param name="nrEnum">The <see cref="NREnum"/> to visit.</param>
    public void Visit(NREnum nrEnum)
    {
      OutputLine(nrEnum.Declaration());
      OutputLine("{");
      indent++;
      foreach (NREnumValue nrEnumValue in nrEnum.Values)
      {
        nrEnumValue.Accept(this);
      }
      indent--;
      OutputLine("}");
    }

    /// <summary>
    /// Visit a <see cref="NRField"/>.
    /// </summary>
    /// <param name="nrField">The <see cref="NRField"/> to visit.</param>
    public void Visit(NRField nrField)
    {
      OutputLine(nrField.Declaration() + ";");
    }

    /// <summary>
    /// Visit a <see cref="NRProperty"/>.
    /// </summary>
    /// <param name="nrProperty">The <see cref="NRProperty"/> to visit.</param>
    public void Visit(NRProperty nrProperty)
    {
      OutputLine(nrProperty.Declaration());
    }

    /// <summary>
    /// Visit a <see cref="NRMethod"/>.
    /// </summary>
    /// <param name="nrMethod">The <see cref="NRMethod"/> to visit.</param>
    public void Visit(NRMethod nrMethod)
    {
      OutputLine(nrMethod.Declaration());
      if (nrMethod.GenericTypes.Count > 0)
      {
        foreach (NRTypeParameter nrTypeParameter in nrMethod.GenericTypes)
        {
          nrTypeParameter.Accept(this);
        }
      }
      OutputLine("{}");
    }

    /// <summary>
    /// Visit a <see cref="NROperator"/>.
    /// </summary>
    /// <param name="nrOperator">The <see cref="NROperator"/> to visit.</param>
    public void Visit(NROperator nrOperator)
    {
      OutputLine(nrOperator.Declaration() + "{}");
    }

    /// <summary>
    /// Visit a <see cref="NRConstructor"/>.
    /// </summary>
    /// <param name="nrConstructor">The <see cref="NRConstructor"/> to visit.</param>
    public void Visit(NRConstructor nrConstructor)
    {
      OutputLine(nrConstructor.Declaration() + "{}");
    }

    /// <summary>
    /// Visit a <see cref="NREvent"/>.
    /// </summary>
    /// <param name="nrEvent">The <see cref="NREvent"/> to visit.</param>
    public void Visit(NREvent nrEvent)
    {
      OutputLine(nrEvent.Declaration() + ";");
    }

    /// <summary>
    /// Visit a <see cref="NRParameter"/>.
    /// </summary>
    /// <param name="nrParameter">The <see cref="NRParameter"/> to visit.</param>
    public void Visit(NRParameter nrParameter)
    {
      OutputLine(nrParameter.Declaration());
    }

    /// <summary>
    /// Visit a <see cref="NRTypeParameter"/>.
    /// </summary>
    /// <param name="nrTypeParameter">The <see cref="NRTypeParameter"/> to visit.</param>
    public void Visit(NRTypeParameter nrTypeParameter)
    {
      string declaration = nrTypeParameter.Declaration();
      if (!string.IsNullOrEmpty(declaration))
      {
        OutputLine(declaration);
      }
    }

    /// <summary>
    /// Visit a <see cref="NREnumValue"/>.
    /// </summary>
    /// <param name="nrEnumValue">The <see cref="NREnumValue"/> to visit.</param>
    public void Visit(NREnumValue nrEnumValue)
    {
      OutputLine(nrEnumValue.Declaration() + ";");
    }

    /// <summary>
    /// Visit a <see cref="NRAttribute"/>.
    /// </summary>
    /// <param name="nrAttribute">The <see cref="NRAttribute"/> to visit.</param>
    public void Visit(NRAttribute nrAttribute)
    {
      OutputLine(nrAttribute.Declaration());
    }

    /// <summary>
    /// Visit a <see cref="NRModule"/>.
    /// </summary>
    /// <param name="nrModule">The <see cref="NRModule"/> to visit.</param>
    public void Visit(NRModule nrModule)
    {
      // Don't do anything...
    }

    #endregion
  }
}