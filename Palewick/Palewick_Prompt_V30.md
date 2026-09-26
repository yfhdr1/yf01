# برومبت Palewick V30 — المرجع الكامل
آخر تحديث: 26/9/2026
Unity 6000.6.1f1 | Android | Photon PUN 2 | IL2CPP | ARM64 | LZ4

(كل قواعد V29 تبقى مثل ما هي: الهوية، ترتيب المشاهد، قواعد الرد، Photon، Unity 6.6 API، الحركة، Animator اللاعب، الكاميرا، الفلاش، البناء والنسخ الاحتياطي.)

## 1. المسارات المضافة بعد V29
### Runtime
- Assets/Scripts/CameraTurnSfx.cs (تم تصليحه: OnEnable/OnDisable/OnDestroy + حماية قسمة صفر)
- Assets/Scripts/DoorController.cs (تم تصليحه: Awake بدل Start، Camera.main، Clamp01، RPC AllBuffered)
- IInteractable: الواجهة يستخدمها DoorController — مسارها غير معروف، اطلبه قبل الاستخدام.

### Editor (يحتاج تأكيد المسار)
- SetupMonsterAnimatorButton.cs — MenuItem: Tools/Monster/Setup Animator Conditions
  - المسار غير معروف بعد.
  - مخالف للقواعد: ما بيه namespace Palewick.EditorTools ويستخدم GameObject.Find.
  - لا تشغّله قبل ما نصلّحه ونتأكد من مساره.

### Monster (الزومبي)
- الموديل: Assets/Monster/Model/Warzombie F Pedroso.fbx
  - Rig: Humanoid | Avatar Definition: Create From This Model
  - الـ Avatar الفرعي موجود: Warzombie F PedrosoAvatar
  - ما بيه Animation Clips داخل الـ FBX.
  - الملفات الفرعية: mixamo.com، mixamorig:Hips، WorldWar_zombie (Mesh)
- التكستشرات: world_war_zombie_diffuse / normal / specular (نفس المجلد)
- المادة: Assets/Monster/Model/WorldWar_zombie_material
- مجلد الأنيميشنات: Assets/Monster/Animations (المحتوى لم يُفحص بعد)
- مجلد عام آخر: Assets/Animations (لم يُفحص)
- الكائن بالمشهد: Monster_AI بـ Scene_A

## 2. PlayerHealth (النص الحالي محفوظ بـ Scripts/PlayerHealth_current.cs)
- TakeDamage(int amount, PhotonMessageInfo info = default) عليه [PunRPC] ويرفض الضرر إذا pv مو IsMine.
- invincibilityTime = 1.
- مشاكل مكتشفة (تنتظر التصليح بطلب):
  1. RPC_Die و OnPhotonSerializeView يفتحون DeathPanel عند اللاعبين الثانيين لما يموت لاعب غيرهم.
  2. UpdateUI للاعب البعيد يغيّر Health Slider المحلي (يبين دم لاعب ثاني).
  3. FindUIElements و BloodEffect ينحلّون حتى للاعب مو المحلي.

## 3. EnemyAI — النسخة الجديدة (Scripts/EnemyAI.cs)
- بدون دوريات: يثبت بمكان الـ Spawn ويحرس.
- يكتشف: أقرب من closeDetectionRange (5) مباشرة، أو ضمن detectionRange (15) مع خط رؤية.
- يطارد بدون توقف، يحدّث الهدف كل 0.1 ثانية، ويهجم واقف أو متحرك.
- يرجع للـ Spawn إذا: اللاعب أبعد من loseTargetRange (25)، أو هو أبعد من leashRange (40) عن مكانه، أو اللاعب بالماي (خارج NavMesh) لمدة 2 ثانية.
- Master Client فقط يحرك؛ الباقين Lerp + مزامنة Speed الأنيميشن عبر IPunObservable.
- الضرر: RPC لكل الأجهزة وصاحب اللاعب فقط ينفّذ PlayerHealth.TakeDamage.
- Animator: Speed = 0 Idle / 0.5 Walk (رجوع) / 1 Run (مطاردة)، Trigger Attack و Die إذا موجودين.
- يحافظ على الـ API القديم: animator، agent، playerTarget، SetPlayer، TakeDamage(float)، Die.
- يحتاج: PhotonView على Monster_AI و EnemyAI مضاف بـ Observed Components.

## 4. المشاكل المعلقة
1. أنيميشن الزومبي ما يشتغل: فحص Animator على Monster_AI (Controller + Avatar = Warzombie F PedrosoAvatar) ومحتوى Assets/Monster/Animations.
2. فحص PhotonView على Monster_AI.
3. تصليح PlayerHealth (الفقرة 2).
4. تصليح SetupMonsterAnimatorButton وتحديد مساره.
5. Runtime/Models مال PostProcessing (15 ملف) — مؤجل.

## 5. فحص Monster_AI (26/9/2026 - 7:28 PM)
- Tag: Enemy | Layer: Default | Static مطفأ | Position 532.79, 17.5, 187.15 | Rotation Y 307.46
- PhotonView: ViewID 133، Fixed، Unreliable On Change، Observed = EnemyAI ✔
- NavMeshAgent: Humanoid، Speed 1.8، Angular 360، Accel 12، Stopping 1.6، Radius 0.5، Height 2.2، High Quality، Area Walkable
- CapsuleCollider: Is Trigger ✔، Center Y 1، Radius 0.5، Height 3
- ZombieSoundController موجود
- Animator: Controller = MonsterAnimator، Avatar = None ❌ (سبب توقف الأنيميشن)، Root Motion مطفأ، Culling Always Animate، Clip Count 4 (Humanoid)
- EnemyAI Inspector: Patrol 1.8، Chase 4.2، Stop 1.6، Attack Range 1.15، Damage 10، Cooldown 2.2، Patrol Radius 6
- أطفال: Warzombie F Pedroso (mixamorig:Hips، WorldWar_zombie) + PatrolPoints (ما يستخدمه الكود الجديد)
