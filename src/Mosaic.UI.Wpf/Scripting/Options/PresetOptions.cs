namespace Mosaic.UI.Wpf.Scripting.Options
{
    public static class PresetOptions
    {
        /// <summary>
        /// Default style
        /// </summary>
        public static ScriptEngineOptions FriendlyStyle =>
            new()
            {
                AllowNullReferenceMemberAccess = true,
                AllowUndefinedReferenceAccess = true,
                AllowUndefinedReferenceMemberAccess = true,
                AssignmentWithoutDefinitionBehavior =
                    AssignmentWithoutDefinitionBehavior.DefineAsVarInExecutionScope,
                NoUndefined = true,
                VarScopeBehavior = VarScopeBehavior.FunctionScope
            };

        public static ScriptEngineOptions EcmaJavascript =>
            new()
            {
                AllowNullReferenceMemberAccess = false,
                AllowUndefinedReferenceAccess = false,
                AllowUndefinedReferenceMemberAccess = false,
                AssignmentWithoutDefinitionBehavior =
                    AssignmentWithoutDefinitionBehavior.DefineAsVarInGlobalScope,
                NoUndefined = false,
                VarScopeBehavior = VarScopeBehavior.FunctionScope
            };
    
        public static ScriptEngineOptions EarlyErrorCatchStyle =>
            new()
            {
                AllowNullReferenceMemberAccess = false,
                AllowUndefinedReferenceAccess = true,
                AllowUndefinedReferenceMemberAccess = false,
                AssignmentWithoutDefinitionBehavior =
                    AssignmentWithoutDefinitionBehavior.DefineAsVarInExecutionScope,
                NoUndefined = true,
                VarScopeBehavior = VarScopeBehavior.FunctionScope
            };
    }
}
