using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    internal static class RszExtensions
    {
        public static RszObjectNode Serialize<T>(this RszTypeRepository repo, T obj)
        {
            return (RszObjectNode)RszSerializer.Serialize(
                repo.FromName(obj!.GetType().FullName!)!,
                obj);
        }

        public static RszGameObject WithGimmickContextId(this RszGameObject gameObject, chainsaw.ContextID contextId)
        {
            var gimmickCore = gameObject.FindComponent("chainsaw.GimmickCore")!;
            gimmickCore = gimmickCore.Set("_ID", contextId);
            gameObject = gameObject.AddOrUpdateComponent(gimmickCore);
            return gameObject;
        }
    }
}
