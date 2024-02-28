using Cesium.Ast;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal sealed record GlobalConstructorScope(TranslationUnitContext Context) : IEmitScope, IDeclarationScope
{
    private MethodDefinition? _method;
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public MethodDefinition Method => _method ??= Context.AssemblyContext.GetGlobalInitializer();
    public TargetArchitectureSet ArchitectureSet => AssemblyContext.ArchitectureSet;
    public FunctionInfo? GetFunctionInfo(string identifier) =>
        Context.GetFunctionInfo(identifier);

    public void DeclareFunction(string identifier, FunctionInfo functionInfo)
        => Context.DeclareFunction(identifier, functionInfo);
    public VariableInfo? GetGlobalField(string identifier) => AssemblyContext.GetGlobalField(identifier);

    private readonly Dictionary<string, VariableInfo> _variables = new();

    private readonly List<object> _specialEffects = new();

    public void AddVariable(StorageClass storageClass, string identifier, IType variableType, IExpression? constant)
    {
        if (constant is not null)
        {
            _variables.Add(identifier, new(identifier, storageClass, variableType, constant));
            return;
        }

        if (storageClass == StorageClass.Static)
        {
            _variables.Add(identifier, new(identifier, storageClass, variableType, constant));
        }

        Context.AddTranslationUnitLevelField(storageClass, identifier, variableType);
    }

    public VariableInfo? GetVariable(string identifier)
    {
        return _variables.GetValueOrDefault(identifier);
    }
    public VariableDefinition ResolveVariable(string identifier) =>
        throw new AssertException("Cannot add a variable into a global constructor scope");

    public ParameterInfo? GetParameterInfo(string name) => null;
    public ParameterDefinition ResolveParameter(int index) =>
        throw new AssertException("Cannot resolve parameter from the global constructor scope");

    /// <inheritdoc />
    public IType ResolveType(IType type) => Context.ResolveType(type);
    public IType? TryGetType(string identifier) => Context.TryGetType(identifier);
    public void AddTypeDefinition(string identifier, IType type) => Context.AddTypeDefinition(identifier, type);
    public void AddTagDefinition(string identifier, IType type) => Context.AddTagDefinition(identifier, type);

    /// <inheritdoc />
    public void AddLabel(string identifier)
    {
        throw new AssertException("Cannot define label into a global constructor scope");
    }

    /// <inheritdoc />
    public Instruction ResolveLabel(string label)
    {
        throw new AssertException("Cannot define label into a global constructor scope");
    }

    /// <inheritdoc />
    public string? GetBreakLabel() => null;

    /// <inheritdoc />
    public string? GetContinueLabel() => null;

    public List<SwitchCase>? SwitchCases => null;

    public void PushSpecialEffect(object declaration) => _specialEffects.Add(declaration);

    public T? GetSpecialEffect<T>()
    {
        for(int i = _specialEffects.Count - 1; i >= 0; i--)
        {
            var effect = _specialEffects[i];
            if (effect is T t)
                return t;
        }
        return default;
    }

    public void RemoveSpecialEffect<T>(Predicate<T> predicate)
    {
        for (int i = _specialEffects.Count - 1; i >= 0; i--)
        {
            var effect = _specialEffects[i];
            if (effect is T t && predicate(t))
            {
                _specialEffects.RemoveAt(i);
                return;
            }
        }
    }
}
