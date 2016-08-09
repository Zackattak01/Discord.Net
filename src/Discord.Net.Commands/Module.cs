﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace Discord.Commands
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class Module
    {
        public CommandService Service { get; }
        public string Name { get; }
        public string Summary { get; }
        public string Description { get; }
        public IEnumerable<Command> Commands { get; }
        internal object Instance { get; }

        public IReadOnlyList<PreconditionAttribute> Preconditions { get; }

        internal Module(CommandService service, object instance, ModuleAttribute moduleAttr, TypeInfo typeInfo)
        {
            Service = service;
            Name = typeInfo.Name;
            Instance = instance;

            var summaryAttr = typeInfo.GetCustomAttribute<SummaryAttribute>();
            if (summaryAttr != null)
                Summary = summaryAttr.Text;

            var descriptionAttr = typeInfo.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttr != null)
                Description = descriptionAttr.Text;

            List<Command> commands = new List<Command>();
            SearchClass(instance, commands, typeInfo, moduleAttr.Prefix ?? "");
            Commands = commands;

            Preconditions = BuildPreconditions(typeInfo);
        }

        private void SearchClass(object instance, List<Command> commands, TypeInfo typeInfo, string groupPrefix)
        {
            if (groupPrefix != "")
                groupPrefix += " ";
            foreach (var method in typeInfo.DeclaredMethods)
            {
                var cmdAttr = method.GetCustomAttribute<CommandAttribute>();
                if (cmdAttr != null)
                    commands.Add(new Command(this, instance, cmdAttr, method, groupPrefix));
            }
            foreach (var type in typeInfo.DeclaredNestedTypes)
            {
                var groupAttrib = type.GetCustomAttribute<GroupAttribute>();
                if (groupAttrib != null)
                {
                    string nextGroupPrefix;
                    if (groupAttrib.Prefix != null)
                        nextGroupPrefix = groupPrefix + groupAttrib.Prefix ?? type.Name;
                    else
                        nextGroupPrefix = groupPrefix;
                    SearchClass(ReflectionUtils.CreateObject(type, Service), commands, type, nextGroupPrefix);
                }
            }
        }

        private IReadOnlyList<PreconditionAttribute> BuildPreconditions(TypeInfo typeInfo)
        {
            return typeInfo.GetCustomAttributes<PreconditionAttribute>().ToImmutableArray();
        }

        public override string ToString() => Name;
        private string DebuggerDisplay => Name;
    }
}
