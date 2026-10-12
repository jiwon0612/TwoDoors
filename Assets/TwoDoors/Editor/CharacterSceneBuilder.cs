using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using TwoDoors.Characters;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace TwoDoors.Editor
{
    public static class CharacterSceneBuilder
    {
        private const string Root = "Assets/TwoDoors";
        private const string Output = Root + "/Generated";
        public const string ScenePath = Root + "/Scenes/CharacterTest.unity";
        private static Material wood, dark, floor, stone, skin;
        private static TMP_FontAsset font;

        [MenuItem("TwoDoors/Create Character Test Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene before building the test scene.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("CharacterTest already exists; open it instead of replacing it.");
            foreach (string folder in new[] {Output, Output + "/Meshes", Output + "/Animations", Output + "/Materials", Root + "/Prefabs", Root + "/Scenes"})
                Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            ConfigureImports();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            wood = Material("Wood", new Color(0.57f, 0.35f, 0.17f));
            dark = Material("GunMetal", new Color(0.055f, 0.065f, 0.075f), 0.65f);
            floor = Material("Concrete", new Color(0.18f, 0.2f, 0.22f));
            stone = Material("Stone", new Color(0.42f, 0.43f, 0.4f));
            skin = Material("Sleeve", new Color(0.16f, 0.2f, 0.16f));
            MakeWoodTexture();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (!font) throw new InvalidOperationException("Import TMP essential resources before building.");
            var environment = new GameObject("TestRoom");
            Box("Floor", environment.transform, new Vector3(0,-0.15f,2), new Vector3(24,0.3f,26), floor);
            Box("BackWall", environment.transform, new Vector3(0,2.5f,15), new Vector3(24,5,0.3f), floor);
            Box("FrontWall", environment.transform, new Vector3(0,2.5f,-11), new Vector3(24,5,0.3f), floor);
            Box("LeftWall", environment.transform, new Vector3(-12,2.5f,2), new Vector3(0.3f,5,26), floor);
            Box("RightWall", environment.transform, new Vector3(12,2.5f,2), new Vector3(0.3f,5,26), floor);
            Box("Obstacle", environment.transform, new Vector3(0,0.6f,3), new Vector3(2,1.2f,1), floor);
            for(int i=-2;i<=2;i++)
                Box("FloorJoint", environment.transform, new Vector3(i*4,0.006f,2), new Vector3(0.025f,0.01f,26), dark, false);
            var surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            AssetDatabase.CreateAsset(surface.navMeshData, Output + "/TestRoomNavMesh.asset");
            var sun = new GameObject("RoomLight").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.4f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48,-25,0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f,0.35f,0.4f);
            for (int i=0;i<3;i++)
            {
                var lamp = new GameObject("CeilingLight").AddComponent<Light>();
                lamp.type = LightType.Point; lamp.range = 12; lamp.intensity = 3;
                lamp.color = new Color(0.8f,0.88f,1); lamp.transform.position = new Vector3((i-1)*7,4,5);
            }
            var mannequin = CreateEnemy<MannequinEnemy>("Mannequin", MakeMannequin(), new Vector3(-5,0,8),100,2.3f,12,0.45f,1.3f,1.6f);
            var golem = CreateEnemy<GolemEnemy>("Golem", MakeGolem(), new Vector3(0,0,10),350,0.65f,30,0.95f,2.3f,2.0f);
            var zombie = CreateEnemy<ZombieEnemy>("Zombie", MakeZombie(), new Vector3(5,0,8),120,1.35f,15,0.65f,1.7f,1.6f);
            var player = MakePlayer();
            MakeHud(player);
            foreach (var enemy in new EnemyCharacter[]{mannequin,golem,zombie}) enemy.transform.rotation = Quaternion.Euler(0,180,0);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var builds = EditorBuildSettings.scenes.ToList();
            if (!builds.Any(s=>s.path == ScenePath)) builds.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes = builds.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log("TwoDoors character test created: 4 prefabs, 3 animated enemies, FPS player, HUD, baked navigation.");
        }

        private static void ConfigureImports()
        {
            foreach (string path in new[]{Root+"/Art/Mannequin/Mannequin.fbx",Root+"/Art/Golem/Golem.fbx",Root+"/Art/Zombie/walk.FBX",Root+"/Art/Zombie/fury.FBX",Root+"/Art/Zombie/dead.FBX"})
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.isReadable = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips) { clip.loopTime = !path.Contains("dead"); clip.lockRootPositionXZ = true; clip.lockRootHeightY = true; clip.lockRootRotation = true; }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        private static Material Material(string name, Color color, float metallic=0)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/"+name+".mat");
            bool isNew = !m;
            if (isNew) m = new Material(Shader.Find("Universal Render Pipeline/Lit")) {name=name};
            m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",0.23f); m.SetFloat("_Metallic",metallic);
            if (isNew) AssetDatabase.CreateAsset(m,Output+"/Materials/"+name+".mat"); return m;
        }

        private static void MakeWoodTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/Materials/WoodGrain.asset");
            if (existing) { wood.SetTexture("_BaseMap",existing); return; }
            var tex = new Texture2D(128,128);
            for(int y=0;y<128;y++) for(int x=0;x<128;x++)
            {
                float grain = Mathf.PerlinNoise(x*0.17f,y*0.009f)*0.35f + 0.65f;
                tex.SetPixel(x,y,new Color(grain,grain,grain));
            }
            tex.Apply(); tex.name="WoodGrain";
            AssetDatabase.CreateAsset(tex,Output+"/Materials/WoodGrain.asset"); wood.SetTexture("_BaseMap",tex);
        }

        private static Transform Joint(string name, Transform parent, Vector3 worldPosition)
        {
            var t = new GameObject(name).transform; t.SetParent(parent,false); t.position=worldPosition; return t;
        }

        private static GameObject MakeMannequin()
        {
            var source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Art/Mannequin/Mannequin.fbx"));
            source.transform.rotation = Quaternion.Euler(0,90,0);
            var rig = new GameObject("Visual");
            var hips = Joint("Hips",rig.transform,new Vector3(0,0.92f,0));
            var chest = Joint("Chest",hips,new Vector3(0,1.25f,0));
            var head = Joint("Head",chest,new Vector3(0,1.48f,0));
            var leftArm = Joint("LeftArm",chest,new Vector3(-0.18f,1.39f,0));
            var rightArm = Joint("RightArm",chest,new Vector3(0.18f,1.39f,0));
            var leftFore = Joint("LeftForearm",leftArm,new Vector3(-0.58f,1.39f,0));
            var rightFore = Joint("RightForearm",rightArm,new Vector3(0.58f,1.39f,0));
            var leftLeg = Joint("LeftLeg",hips,new Vector3(-0.1f,0.86f,0));
            var rightLeg = Joint("RightLeg",hips,new Vector3(0.1f,0.86f,0));
            var leftKnee = Joint("LeftKnee",leftLeg,new Vector3(-0.1f,0.45f,0));
            var rightKnee = Joint("RightKnee",rightLeg,new Vector3(0.1f,0.45f,0));
            foreach(var f in source.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=f.sharedMesh; var vertices=mesh.vertices; var uv=mesh.uv; var triangles=mesh.triangles;
                var groups=new Dictionary<Transform,List<int>>();
                Vector3 World(int v) => f.transform.TransformPoint(vertices[v])+new Vector3(0,0.08f,0);
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector3 center=(World(triangles[i])+World(triangles[i+1])+World(triangles[i+2]))/3;
                    Transform joint = f.name=="HEAD"||f.name=="NEQUE" ? head : f.name=="CHEST" ? chest : hips;
                    if(f.name=="HAND") joint=center.x<0 ? (center.x < -0.58f ? leftFore:leftArm) : (center.x>0.58f?rightFore:rightArm);
                    if(f.name=="LEG") joint=center.x<0 ? (center.y<0.45f?leftKnee:leftLeg) : (center.y<0.45f?rightKnee:rightLeg);
                    if(!groups.ContainsKey(joint)) groups[joint]=new List<int>();
                    groups[joint].AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
                }
                foreach(var group in groups)
                {
                    var points=new List<Vector3>(); var uvs=new List<Vector2>(); var indices=new List<int>();
                    foreach(int v in group.Value) { indices.Add(points.Count); points.Add(group.Key.InverseTransformPoint(World(v))); uvs.Add(uv.Length>v?uv[v]:Vector2.zero); }
                    var part=new Mesh{name=f.name+group.Key.name}; part.SetVertices(points); part.SetUVs(0,uvs); part.SetTriangles(indices,0); part.RecalculateNormals(); part.RecalculateBounds();
                    AssetDatabase.CreateAsset(part,Output+"/Meshes/"+part.name+".asset");
                    var go=new GameObject(f.name); go.transform.SetParent(group.Key,false); go.AddComponent<MeshFilter>().sharedMesh=part; go.AddComponent<MeshRenderer>().sharedMaterial=wood;
                }
            }
            UnityEngine.Object.DestroyImmediate(source);
            leftArm.localRotation=Quaternion.Euler(0,0,75); rightArm.localRotation=Quaternion.Euler(0,0,-75);
            AddRigAnimations(rig, "Mannequin", new[]{hips,chest,head,leftArm,rightArm,leftLeg,rightLeg},false);
            return rig;
        }

        private static GameObject MakeGolem()
        {
            var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Art/Golem/Golem.fbx")); model.name="Visual";
            foreach(var r in model.GetComponentsInChildren<Renderer>())
            {
                var materials=r.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var m=Material("Golem_"+i,new Color(0.65f,0.65f,0.62f));
                    var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Golem/textures/Rock_034_SD/Rock_034_Base_Color.jpg");
                    m.SetTexture("_BaseMap",tex); materials[i]=m;
                }
                r.sharedMaterials=materials;
            }
            Transform Find(string n)=>model.GetComponentsInChildren<Transform>().First(t=>t.name==n);
            var la=Find("Left arm"); var ra=Find("Right arm");
            la.rotation=Quaternion.AngleAxis(la.position.x<0?70:-70,Vector3.forward)*la.rotation;
            ra.rotation=Quaternion.AngleAxis(ra.position.x<0?70:-70,Vector3.forward)*ra.rotation;
            AddRigAnimations(model,"Golem",new[]{Find("Hips"),Find("Chest"),Find("Head"),la,ra,Find("Left leg"),Find("Right leg")},true);
            model.transform.localScale=Vector3.one*1.45f; model.transform.localPosition=Vector3.up*0.15f;
            return model;
        }

        private static void AddRigAnimations(GameObject model,string name,Transform[] bones,bool heavy)
        {
            var clips=new List<AnimationClip>();
            foreach(string state in new[]{"Idle","Walk","Attack","Death"})
            {
                float length=state=="Attack"?(heavy?2.1f:1.1f):state=="Death"?1.2f:heavy?2f:1f;
                var clip=new AnimationClip{name=state,frameRate=30};
                for(int b=0;b<bones.Length;b++)
                {
                    var t=bones[b]; var rest=t.localRotation;
                    var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
                    for(int k=0;k<=30;k++)
                    {
                        float phase=k/30f; float angle=0;
                        if(state=="Idle") angle=b==1?Mathf.Sin(phase*Mathf.PI*2)*1.5f:0;
                        if(state=="Walk") angle=Mathf.Sin(phase*Mathf.PI*2)*(b>=5?22:b>=3?12:b==1?3:0)*(b%2==0?1:-1);
                        if(state=="Attack") angle=Mathf.Sin(phase*Mathf.PI)*(b==3||b==4?-100:b==1?15:0);
                        if(state=="Death") angle=b==0?Mathf.SmoothStep(0,85,phase):b>=3?25*phase:0;
                        var parent=t.parent.rotation;
                        Quaternion q=Quaternion.Inverse(parent)*Quaternion.AngleAxis(angle,Vector3.right)*parent*rest;
                        curves[0].AddKey(phase*length,q.x); curves[1].AddKey(phase*length,q.y); curves[2].AddKey(phase*length,q.z); curves[3].AddKey(phase*length,q.w);
                    }
                    string path=AnimationUtility.CalculateTransformPath(t,model.transform);
                    for(int c=0;c<4;c++) clip.SetCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[c],curves[c]);
                }
                clip.EnsureQuaternionContinuity();
                var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=state=="Idle"||state=="Walk"; AnimationUtility.SetAnimationClipSettings(clip,settings);
                AssetDatabase.CreateAsset(clip,Output+"/Animations/"+name+state+".anim"); clips.Add(clip);
            }
            var animator=model.GetComponent<Animator>();
            if (!animator) animator=model.AddComponent<Animator>();
            animator.applyRootMotion=false; animator.runtimeAnimatorController=Controller(name,clips.ToArray());
        }

        private static GameObject MakeZombie()
        {
            string path=Root+"/Art/Zombie/";
            var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path+"walk.FBX")); model.name="Visual";
            var material=Material("Zombie",Color.white);
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"difusse.jpg"));
            foreach(var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial=material;
            var imported=new List<AnimationClip>();
            foreach(string file in new[]{"walk","fury","dead"})
            {
                var source=AssetDatabase.LoadAllAssetsAtPath(path+file+".FBX").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
                var clip=UnityEngine.Object.Instantiate(source); clip.name=file;
                // The source clips contain translational root motion; navigation owns position.
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                    if(binding.path=="Bip01" && binding.propertyName.StartsWith("m_LocalPosition"))
                    { var curve=AnimationUtility.GetEditorCurve(clip,binding); float value=curve.Evaluate(0); AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,clip.length,value)); }
                var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=file=="walk"; AnimationUtility.SetAnimationClipSettings(clip,settings);
                AssetDatabase.CreateAsset(clip,Output+"/Animations/Zombie"+file+".anim"); imported.Add(clip);
            }
            var idle=UnityEngine.Object.Instantiate(imported[0]); idle.name="Idle";
            foreach(var binding in AnimationUtility.GetCurveBindings(idle))
            { var curve=AnimationUtility.GetEditorCurve(idle,binding); AnimationUtility.SetEditorCurve(idle,binding,AnimationCurve.Constant(0,1,curve.Evaluate(0))); }
            AssetDatabase.CreateAsset(idle,Output+"/Animations/ZombieIdle.anim");
            var animator=model.GetComponent<Animator>();
            if (!animator) animator=model.AddComponent<Animator>();
            animator.applyRootMotion=false;
            animator.runtimeAnimatorController=Controller("Zombie",new[]{idle,imported[0],imported[1],imported[2]});
            var bounds=model.GetComponentInChildren<Renderer>().bounds;
            float scale=1.8f/bounds.size.y; model.transform.localScale=Vector3.one*scale;
            model.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
            return model;
        }

        private static AnimatorController Controller(string name,AnimationClip[] clips)
        {
            var controller=AnimatorController.CreateAnimatorControllerAtPath(Output+"/Animations/"+name+".controller");
            controller.AddParameter("Moving",AnimatorControllerParameterType.Bool); controller.AddParameter("Attack",AnimatorControllerParameterType.Trigger); controller.AddParameter("Dead",AnimatorControllerParameterType.Bool);
            var sm=controller.layers[0].stateMachine; var states=new AnimatorState[4];
            for(int i=0;i<4;i++){ states[i]=sm.AddState(new[]{"Idle","Walk","Attack","Death"}[i]); states[i].motion=clips[i]; }
            sm.defaultState=states[0];
            void Transition(AnimatorState from,AnimatorState to,AnimatorConditionMode mode,string parameter)
            {var t=from.AddTransition(to);t.hasExitTime=false;t.duration=0.12f;t.AddCondition(mode,0,parameter);}
            Transition(states[0],states[1],AnimatorConditionMode.If,"Moving"); Transition(states[1],states[0],AnimatorConditionMode.IfNot,"Moving");
            var attack=sm.AddAnyStateTransition(states[2]); attack.hasExitTime=false; attack.duration=0.08f; attack.canTransitionToSelf=false; attack.AddCondition(AnimatorConditionMode.If,0,"Attack"); attack.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");
            var back=states[2].AddTransition(states[0]);back.hasExitTime=true;back.exitTime=0.95f;back.duration=0.1f;
            var death=sm.AddAnyStateTransition(states[3]);death.hasExitTime=false;death.duration=0.12f;death.canTransitionToSelf=false;death.AddCondition(AnimatorConditionMode.If,0,"Dead");
            return controller;
        }

        private static T CreateEnemy<T>(string name,GameObject visual,Vector3 position,int hp,float speed,int damage,float windup,float cooldown,float range) where T:EnemyCharacter
        {
            var root=new GameObject(name); visual.transform.SetParent(root.transform,false);
            var agent=root.AddComponent<NavMeshAgent>(); agent.speed=speed; agent.angularSpeed=240; agent.acceleration=10; agent.radius=name=="Golem"?0.65f:0.32f; agent.height=name=="Golem"?2.8f:1.85f;agent.stoppingDistance=range*0.8f;
            var collider=root.AddComponent<CapsuleCollider>();collider.height=agent.height;collider.radius=agent.radius;collider.center=Vector3.up*agent.height*0.5f;
            var enemy=root.AddComponent<T>(); Set(enemy,"maxHealth",hp);Set(enemy,"animator",visual.GetComponent<Animator>());Set(enemy,"attackDamage",damage);Set(enemy,"windup",windup);Set(enemy,"attackCooldown",cooldown);Set(enemy,"attackRange",range);
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+name+".prefab"); root.transform.position=position;return enemy;
        }

        private static PlayerCharacter MakePlayer()
        {
            var go=new GameObject("Player");go.tag="Player";
            var motor=go.AddComponent<CharacterController>();motor.height=1.8f;motor.radius=0.3f;motor.center=Vector3.up*0.9f;motor.stepOffset=0.3f;
            var player=go.AddComponent<PlayerCharacter>();
            var camera=new GameObject("PlayerCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(go.transform,false);camera.transform.localPosition=Vector3.up*1.65f;camera.nearClipPlane=0.03f;camera.fieldOfView=70;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.025f,0.03f,0.04f);camera.gameObject.AddComponent<AudioListener>();
            var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Mannequin.prefab")).transform.Find("Visual").gameObject;
            var temp=body.transform.parent.gameObject; body.transform.SetParent(go.transform,false);UnityEngine.Object.DestroyImmediate(temp);
            body.name="PlayerBody";foreach(var r in body.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
            var hands=new GameObject("HandsAndWeapon").transform;hands.SetParent(camera.transform,false);hands.localPosition=new Vector3(0.22f,-0.25f,0.45f);
            Box("RightSleeve",hands,new Vector3(0,-0.15f,-0.06f),new Vector3(0.12f,0.13f,0.42f),skin,false);
            Box("LeftSleeve",hands,new Vector3(-0.16f,-0.16f,-0.08f),new Vector3(0.1f,0.12f,0.38f),skin,false);
            var pistol=new GameObject("Pistol");pistol.transform.SetParent(hands,false);
            Box("Slide",pistol.transform,new Vector3(0,0,0.05f),new Vector3(0.06f,0.07f,0.26f),dark,false);
            Box("Grip",pistol.transform,new Vector3(0,-0.07f,-0.03f),new Vector3(0.055f,0.14f,0.075f),dark,false);
            var knife=new GameObject("Knife");knife.transform.SetParent(hands,false);
            Box("Handle",knife.transform,Vector3.zero,new Vector3(0.04f,0.04f,0.13f),dark,false);
            Box("Blade",knife.transform,new Vector3(0,0,0.16f),new Vector3(0.008f,0.055f,0.2f),Material("Blade",new Color(0.65f,0.7f,0.72f),0.85f),false);
            var light=new GameObject("Flashlight").AddComponent<Light>();light.type=LightType.Spot;light.range=22;light.spotAngle=48;light.intensity=5;light.shadows=LightShadows.Soft;light.transform.SetParent(camera.transform,false);
            var flash=new GameObject("MuzzleFlash").AddComponent<Light>();flash.type=LightType.Point;flash.color=new Color(1,0.65f,0.2f);flash.intensity=5;flash.range=4;flash.transform.SetParent(hands,false);flash.transform.localPosition=Vector3.forward*0.3f;flash.enabled=false;
            Set(player,"viewCamera",camera);Set(player,"weaponRoot",hands);Set(player,"pistol",pistol);Set(player,"knife",knife);Set(player,"flashlight",light);Set(player,"muzzleFlash",flash);
            RefineFirstPerson(player);
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Player.prefab");go.transform.position=new Vector3(0,0,-6);return player;
        }

        public static void RefineFirstPerson(PlayerCharacter player)
        {
            var hands=player.transform.Find("PlayerCamera/HandsAndWeapon");
            if (hands.Find("RightGlove")) return;
            var glove=Material("Glove",new Color(0.12f,0.105f,0.085f));
            var metal=Material("GunMetal",new Color(0.055f,0.065f,0.075f),0.65f);
            void Rounded(string name,Vector3 position,Vector3 scale)
            {
                var part=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                part.name=name; part.transform.SetParent(hands,false);
                part.transform.localPosition=position;part.transform.localScale=scale;
                part.GetComponent<Renderer>().sharedMaterial=glove;
                UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            }
            Rounded("RightGlove",new Vector3(0,-0.085f,-0.045f),new Vector3(0.085f,0.105f,0.12f));
            Rounded("LeftGlove",new Vector3(-0.065f,-0.075f,0.005f),new Vector3(0.08f,0.09f,0.12f));
            for(int i=0;i<4;i++) Rounded("Finger"+i,new Vector3(-0.015f,-0.045f-i*0.018f,0.01f),new Vector3(0.075f,0.017f,0.038f));
            var pistol=hands.Find("Pistol");
            Box("Barrel",pistol,new Vector3(0,-0.013f,0.19f),new Vector3(0.033f,0.033f,0.04f),metal,false);
            Box("FrontSight",pistol,new Vector3(0,0.043f,0.15f),new Vector3(0.012f,0.02f,0.025f),metal,false);
            Box("RearSight",pistol,new Vector3(0,0.043f,-0.06f),new Vector3(0.05f,0.016f,0.015f),metal,false);
            hands.localPosition=new Vector3(0.17f,-0.22f,0.52f);
            hands.Find("LeftSleeve").localRotation=Quaternion.Euler(0,20,0);
        }

        private static void MakeHud(PlayerCharacter player)
        {
            var canvas=new GameObject("HUD",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=0.5f;
            var demo=canvas.AddComponent<CharacterDemo>();Set(demo,"player",player);
            var overlay=Panel("DamageOverlay",canvas.transform,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(1920,1080),Color.clear);
            Set(demo,"damageOverlay",overlay);
            var hp=Panel("HealthPanel",canvas.transform,new Vector2(0,0),new Vector2(220,95),new Vector2(240,48),new Color(0,0.03f,0.02f,0.65f));
            Set(demo,"health",Text("Health",hp.transform,"100",Vector2.zero,new Vector2(210,45),28));
            var ammo=Panel("AmmoPanel",canvas.transform,new Vector2(1,0),new Vector2(-160,65),new Vector2(220,48),new Color(0,0,0,0.6f));
            Set(demo,"ammo",Text("Ammo",ammo.transform,"7 / 56",Vector2.zero,new Vector2(210,45),28));
            var slotImages=new List<UnityEngine.UI.Image>();
            string[] labels={"1\nKNIFE","2\nPISTOL","3\nHEAL","4\nLIGHT"};
            for(int i=0;i<4;i++){var slot=Panel("Slot"+(i+1),canvas.transform,new Vector2(0.5f,0),new Vector2((i-1.5f)*100,100),new Vector2(88,72),new Color(0,0,0,0.5f));Text("Label",slot.transform,labels[i],Vector2.zero,new Vector2(84,68),20);slotImages.Add(slot);}
            var so=new SerializedObject(demo);var arr=so.FindProperty("slots");arr.arraySize=4;for(int i=0;i<4;i++)arr.GetArrayElementAtIndex(i).objectReferenceValue=slotImages[i];so.ApplyModifiedPropertiesWithoutUndo();
            Panel("Crosshair",canvas.transform,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(4,4),Color.white);
            var marker=Panel("HitMarker",canvas.transform,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(12,12),new Color(1,0.2f,0.1f));marker.enabled=false;Set(demo,"hitMarker",marker);
            Set(demo,"status",Text("Status",canvas.transform,"",new Vector2(0,100),new Vector2(600,140),48));
            Set(demo,"targetLabel",Text("Target",canvas.transform,"",new Vector2(0,-45),new Vector2(550,40),22));
            var help=Text("Controls",canvas.transform,"WASD  Move     SHIFT  Sprint     LMB  Attack     R  Reload     1 / 2  Weapons     3  Heal     F  Light     ESC  Pause     F5  Restart",Vector2.zero,new Vector2(1800,40),19);
            help.rectTransform.anchorMin=help.rectTransform.anchorMax=new Vector2(0.5f,0);help.rectTransform.anchoredPosition=new Vector2(0,24);
            var title=Text("Title",canvas.transform,"TWO DOORS   /   CHARACTER TEST",Vector2.zero,new Vector2(750,45),24);title.rectTransform.anchorMin=title.rectTransform.anchorMax=new Vector2(0.5f,1);title.rectTransform.anchoredPosition=new Vector2(0,-40);
        }

        private static UnityEngine.UI.Image Panel(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=position;r.sizeDelta=size;var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;}
        private static TMP_Text Text(string name,Transform parent,string value,Vector2 position,Vector2 size,float fontSize)
        {var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=fontSize;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.rectTransform.sizeDelta=size;text.rectTransform.anchoredPosition=position;return text;}
        private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material,bool collider=true)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        private static void Set(UnityEngine.Object target,string property,object value)
        {var so=new SerializedObject(target);var p=so.FindProperty(property);if(value is int i)p.intValue=i;else if(value is float f)p.floatValue=f;else if(value is UnityEngine.Object o)p.objectReferenceValue=o;else throw new ArgumentException(property);so.ApplyModifiedPropertiesWithoutUndo();}
    }
}
