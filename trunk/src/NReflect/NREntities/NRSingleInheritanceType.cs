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
using NReflect.NRMembers;

namespace NReflect.NREntities
{
  /// <summary>
  /// Represents a type which can only have one parent type which is reflected by NReflect.
  /// </summary>
  [Serializable]
  public abstract class NRSingleInheritanceType : NRCompositeType
  {
    // ========================================================================
    // Con- / Destruction

    #region === Con- / Destruction

    /// <summary>
    /// Initializes a new instance of <see cref="NRSingleInheritanceType"/>.
    /// </summary>
    protected NRSingleInheritanceType()
    {
      Fields = new List<NRField>();
      NestedTypes = new List<NRTypeBase>();
      Constructors = new List<NRConstructor>();
      Operators = new List<NROperator>();
      ImplementedInterfaces = new List<string>();
    }

    #endregion

    // ========================================================================
    // Properties

    #region === Properties

    /// <summary>
    /// Gets or sets the full name of the base type of this type.
    /// </summary>
    public string BaseType { get; set; }

    /// <summary>
    /// Gets a list which contains the full names of all implemented interfaces.
    /// </summary>
    public List<string> ImplementedInterfaces { get; private set; }

    /// <summary>
    /// Gets a list of nested types.
    /// </summary>
    public List<NRTypeBase> NestedTypes { get; private set; }

    /// <summary>
    /// Gets a list of constructors.
    /// </summary>
    public List<NRConstructor> Constructors { get; private set; }

    /// <summary>
    /// Gets a list of operators of this type.
    /// </summary>
    public List<NROperator> Operators { get; private set; }

    /// <summary>
    /// Gets a list of fields of this type.
    /// </summary>
    public List<NRField> Fields { get; private set; }

    #endregion
  }
}