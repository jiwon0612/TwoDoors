using UnityEditor;

namespace TwoDoors.EditorTools
{
    static class StageChoiceAssetGenerator
    {
        [MenuItem("Tools/TwoDoors/Generate Stage Choices")]
        internal static void Generate() => EventCatalogV2.Generate();
    }
}
