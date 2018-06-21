﻿using MagicOnion.CodeGenerator;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MagicOnion.CodeAnalysis
{
    public class ReferenceSymbols
    {
        public readonly INamedTypeSymbol Task;
        public readonly INamedTypeSymbol TaskOfT;
        public readonly INamedTypeSymbol UnaryResult;
        public readonly INamedTypeSymbol ClientStreamingResult;
        public readonly INamedTypeSymbol ServerStreamingResult;
        public readonly INamedTypeSymbol DuplexStreamingResult;
        public readonly INamedTypeSymbol GenerateDefineIf;

        public ReferenceSymbols(Compilation compilation)
        {
            TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            UnaryResult = compilation.GetTypeByMetadataName("MagicOnion.UnaryResult`1");
            ClientStreamingResult = compilation.GetTypeByMetadataName("MagicOnion.ClientStreamingResult`2");
            DuplexStreamingResult = compilation.GetTypeByMetadataName("MagicOnion.DuplexStreamingResult`2");
            ServerStreamingResult = compilation.GetTypeByMetadataName("MagicOnion.ServerStreamingResult`1");
            GenerateDefineIf = compilation.GetTypeByMetadataName("MagicOnion.GenerateDefineIfAttribute");
        }
    }

    public class MethodCollector
    {
        static readonly SymbolDisplayFormat binaryWriteFormat = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

        static readonly SymbolDisplayFormat shortTypeNameFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

        readonly string csProjPath;
        readonly INamedTypeSymbol[] serviceTypes;
        readonly INamedTypeSymbol[] interfaces;
        readonly ReferenceSymbols typeReferences;
        readonly INamedTypeSymbol baseInterface;

        protected MethodCollector(string csProjPath, Compilation compilation)
        {
            this.csProjPath = csProjPath;
            this.typeReferences = new ReferenceSymbols(compilation);

            var marker = compilation.GetTypeByMetadataName("MagicOnion.IServiceMarker");

            baseInterface = compilation.GetTypeByMetadataName("MagicOnion.IService`1");

            serviceTypes = compilation.GetNamedTypeSymbols()
                .Where(t => t.AllInterfaces.Any(x => x == marker))
                .ToArray();

            interfaces = serviceTypes
                .Concat(serviceTypes.SelectMany(x => x.AllInterfaces))
                .Distinct()
                .Where(x => x != marker)
                .Where(t => t != baseInterface)
                .Where(x => !x.IsGenericType || x.ConstructedFrom != baseInterface)
                .ToArray();
        }

        public static async Task<MethodCollector> CreateCollector(string csProjPath,IEnumerable<string> conditinalSymbols,
            string framework,bool quiet)
        {
            MSBuildLocator.RegisterDefaults();

            var compilation = await RoslynExtensions.GetCompilationFromProject(csProjPath,framework,quiet,
                conditinalSymbols.ToArray());

            return new MethodCollector(csProjPath,compilation);
        }

        // not visitor pattern:)
        public InterfaceDefinition[] Visit()
        {
            return interfaces
                .Select(x => new InterfaceDefinition()
                {
                    Name = x.ToDisplayString(shortTypeNameFormat),
                    Namespace = x.ContainingNamespace.IsGlobalNamespace ? null : x.ContainingNamespace.ToDisplayString(),
                    IsServiceDifinition = false,
                    InterfaceNames = x.Interfaces.Select(y => y.ToDisplayString()).ToArray(),
                    IsIfDebug = x.GetAttributes().FindAttributeShortName("GenerateDefineDebugAttribute") != null,
                    Methods = x.GetMembers()
                        .OfType<IMethodSymbol>()
                        .Select(CreateMethodDefinition)
                        .ToArray()
                })
                .Concat(serviceTypes
                    .Select(x => new InterfaceDefinition
                    {
                        Name = x.ToDisplayString(shortTypeNameFormat),
                        Namespace = x.ContainingNamespace.IsGlobalNamespace ? null : x.ContainingNamespace.ToDisplayString(),
                        IsServiceDifinition = true,
                        InterfaceNames = new string[0],
                        IsIfDebug = x.GetAttributes().FindAttributeShortName("GenerateDefineDebugAttribute") != null,
                        Methods = x.GetAllInterfaceMembers() //with base interface method
                            .OfType<IMethodSymbol>()
                            .Distinct(MethodNameComparer.Instance)
                            .Where(y => y.ContainingType.ConstructedFrom != baseInterface)
                            .Select(CreateMethodDefinition)
                            .ToArray()
                    }))
                .ToArray();
        }

        private class MethodNameComparer : IEqualityComparer<IMethodSymbol>
        {
            public static IEqualityComparer<IMethodSymbol> Instance { get; } = new MethodNameComparer();
            public bool Equals(IMethodSymbol x, IMethodSymbol y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(IMethodSymbol obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        private MethodDefinition CreateMethodDefinition(IMethodSymbol y)
        {
            MethodType t;
            string requestType;
            string responseType;
            ITypeSymbol unwrappedOriginalResponseType;
            ExtractRequestResponseType(y, out t, out requestType, out responseType, out unwrappedOriginalResponseType);
            return new MethodDefinition
            {
                Name = y.Name,
                MethodType = t,
                RequestType = requestType,
                ResponseType = responseType,
                UnwrappedOriginalResposneTypeSymbol = unwrappedOriginalResponseType,
                IsIfDebug = y.GetAttributes().FindAttributeShortName("GenerateDefineDebugAttribute") != null,
                Parameters = y.Parameters.Select(p =>
                {
                    return new ParameterDefinition
                    {
                        ParameterName = p.Name,
                        TypeName = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        HasDefaultValue = p.HasExplicitDefaultValue,
                        DefaultValue = GetDefaultValue(p),
                        OriginalSymbol = p
                    };
                }).ToArray()
            };
        }

        void ExtractRequestResponseType(IMethodSymbol method, out MethodType methodType, out string requestType, out string responseType, out ITypeSymbol unwrappedOriginalResponseType)
        {
            var retType = method.ReturnType as INamedTypeSymbol;

            var constructedFrom = retType.ConstructedFrom;
            if (constructedFrom == typeReferences.TaskOfT)
            {
                retType = retType.TypeArguments[0] as INamedTypeSymbol;
                constructedFrom = retType.ConstructedFrom;
            }

            if (constructedFrom == typeReferences.UnaryResult)
            {
                methodType = MethodType.Unary;
                requestType = (method.Parameters.Length == 1) ? method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null;
                responseType = retType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                unwrappedOriginalResponseType = retType.TypeArguments[0];
            }
            else if (constructedFrom == typeReferences.ServerStreamingResult)
            {
                methodType = MethodType.ServerStreaming;
                requestType = (method.Parameters.Length == 1) ? method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null;
                responseType = retType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                unwrappedOriginalResponseType = retType.TypeArguments[0];
            }
            else if (constructedFrom == typeReferences.ClientStreamingResult)
            {
                methodType = MethodType.ClientStreaming;
                requestType = retType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                responseType = retType.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                unwrappedOriginalResponseType = retType.TypeArguments[1];
            }
            else if (constructedFrom == typeReferences.DuplexStreamingResult)
            {
                methodType = MethodType.DuplexStreaming;
                requestType = retType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                responseType = retType.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                unwrappedOriginalResponseType = retType.TypeArguments[1];
            }
            else
            {
                throw new Exception("Invalid Return Type, method:" + method.Name + " returnType:" + method.ReturnType);
            }
        }

        string GetDefaultValue(IParameterSymbol p)
        {
            if (p.HasExplicitDefaultValue)
            {
                var ppp = p.ToDisplayParts(new SymbolDisplayFormat(parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue));

                if (!ppp.Any(x => x.Kind == SymbolDisplayPartKind.Keyword && x.ToString() == "default"))
                {
                    var l = ppp.Last();
                    if (l.Kind == SymbolDisplayPartKind.FieldName)
                    {
                        return l.Symbol.ToDisplayString();
                    }
                    else
                    {
                        return l.ToString();
                    }
                }
            }

            return "default(" + p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")";
        }
    }
}