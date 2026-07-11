# architecture
- Avoid hardcoded values in source files; all configurable parameters must be exposed through editor tooling (serialized fields, ScriptableObjects, editor dashboard). Confidence: 0.90

# unity-input
- Use new Input System API (UnityEngine.InputSystem.Keyboard) instead of legacy Input.GetKeyDown, as the project has switched to the Input System package. Confidence: 0.70

# csharp
- When rewriting entire C# files, preserve all necessary using directives from the original file to avoid compilation errors. Confidence: 0.70
- When implementing C# interfaces, implement ALL interface members — check the interface definition to ensure no methods are missed (e.g., IGameState requires OnEnterAsync, OnExitAsync, and OnTick). Confidence: 0.70

# workflow
- After completing development work, re-audit and report the current quality state across all categories (architecture, gameplay, UI, performance). Confidence: 0.70
- Iteratively refine until every quality category reaches A+ — don't stop at the first pass; re-audit, fix issues, and repeat. Confidence: 0.85
- After completing development work, verify the code compiles before declaring completion — fix all compilation errors first. Confidence: 0.80
