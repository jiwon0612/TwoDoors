using UnityEngine;

namespace TwoDoors.Items
{
    // Vector silhouettes stay sharp at any Canvas scale, without font glyph dependencies.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ItemIcon : UnityEngine.UI.MaskableGraphic
    {
        public ItemKind kind;
        Color ink;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            ink = color;
            switch (kind)
            {
                case ItemKind.Knife:
                    Quad(mesh, -32,-5,-9,5); Quad(mesh,-12,-10,-8,10);
                    Triangle(mesh,new Vector2(-8,-5),new Vector2(-8,8),new Vector2(33,8)); break;
                case ItemKind.Machete:
                    Quad(mesh,-34,-6,-16,4); Quad(mesh,-18,-11,-14,10);
                    Quad(mesh,-14,-6,23,10); Triangle(mesh,new Vector2(23,-6),new Vector2(23,10),new Vector2(36,15)); break;
                case ItemKind.Pistol:
                    Quad(mesh,-22,0,27,12); Quad(mesh,-20,-20,-9,0); Quad(mesh,-8,-9,5,-5); Quad(mesh,2,-5,5,0); Quad(mesh,19,12,23,16); break;
                case ItemKind.MachineGun:
                    Quad(mesh,-23,0,20,11); Quad(mesh,20,4,36,8); Quad(mesh,-35,-3,-23,8);
                    Quad(mesh,-15,-16,-8,0); Quad(mesh,4,-20,14,0); Quad(mesh,-2,11,9,16); break;
                case ItemKind.Shotgun:
                    Quad(mesh,-18,4,37,10); Quad(mesh,-15,-3,6,4); Quad(mesh,6,-6,24,3);
                    Quad(mesh,-34,-10,-18,4); Quad(mesh,-17,-14,-11,-3); break;
                case ItemKind.Medkit:
                    Quad(mesh,-23,-18,23,14); Quad(mesh,-12,14,-7,21); Quad(mesh,7,14,12,21); Quad(mesh,-12,19,12,23);
                    ink=new Color(.06f,.1f,.13f,color.a);
                    Quad(mesh,-4,-12,4,9); Quad(mesh,-11,-5,11,2); break;
                case ItemKind.Flashlight:
                    Quad(mesh,-29,-7,7,7); Quad(mesh,7,-12,15,12); Quad(mesh,-33,-5,-29,5);
                    Quad(mesh,21,-2,35,2); Quad(mesh,23,12,33,15); Quad(mesh,23,-15,33,-12); break;
            }
        }
        void Quad(UnityEngine.UI.VertexHelper mesh,float x1,float y1,float x2,float y2)
        {
            Triangle(mesh,new Vector2(x1,y1),new Vector2(x1,y2),new Vector2(x2,y2));
            Triangle(mesh,new Vector2(x1,y1),new Vector2(x2,y2),new Vector2(x2,y1));
        }
        void Triangle(UnityEngine.UI.VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c)
        {
            int index=mesh.currentVertCount; var rect=rectTransform.rect;
            foreach(var p in new[]{a,b,c})
                mesh.AddVert(new Vector3(rect.center.x+p.x*rect.width/80f,rect.center.y+p.y*rect.height/50f),ink,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2);
        }
    }
}
