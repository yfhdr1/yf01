برومبت Palewick V30 — المرجع الكامل للمشروع
آخر تحديث: 26/9/2026
Unity 6000.6.1f1 | Android | Photon PUN 2 | IL2CPP | ARM64 | LZ4

1. هوية المشروع
اللعبة: Palewick، Horror Multiplayer، Android فقط، Photon PUN 2.
Unity: 6000.6.1f1.
Scripting Backend: IL2CPP.
Architecture: ARM64.
Compression: LZ4.
Input System: Old Input Manager فقط.
New Input System ممنوع نهائياً.
مسار المشروع: C:\Users\yf_hdr\Desktop\yarekam
النسخة الاحتياطية: لا توجد (المستخدم ما يريد أي نسخة احتياطية بأي مكان، لا تقترحها ولا تسويها).
ناتج البناء الدائم: C:\Users\yf_hdr\Desktop\apkk\Palewick_v1.0.apk
كل Build جديد يستبدل نفس ملف APK.

2. ترتيب المشاهد
Assets/a.loby/Scene_Intro
Assets/a.loby/Scene_Lobby
Assets/a.last/Flooded_Grounds/Scenes/Scene_A

3. سكربتات Runtime ومساراتها
Assets/Scripts/NetworkManager.cs
Assets/Scripts/ServerBrowser.cs
Assets/Scripts/PlayerSetup.cs
Assets/a.last/Flooded_Grounds/Scripts/FlashlightController.cs
Assets/Scripts/PauseMenuPUBG.cs
Assets/Scripts/PlayerQuickChat.cs
Assets/Scripts/PlayerHealth.cs
Assets/Scripts/EnemyAI.cs
Assets/Scripts/SceneLoadingController.cs
Assets/a.last/Flooded_Grounds/Scripts/StaminaSystem.cs
Assets/a.last/Flooded_Grounds/Scripts/SprintButton.cs
Assets/Scripts/PlayerInteraction.cs
Assets/Scripts/BloodEffectUI.cs
Assets/Scripts/ZombieSoundController.cs
Assets/Scripts/FootstepSoundController.cs
Assets/Scripts/CameraTurnSfx.cs
Assets/Scripts/DoorController.cs
Assets/a.loby/Scripts/LobbyManager.cs
Assets/a.last/Flooded_Grounds/Scripts/CameraViewSwitcher.cs
Assets/a.last/Flooded_Grounds/Scripts/FPSController/CharController_Motor.cs
IInteractable: واجهة يستخدمها DoorController، مسارها غير معروف، اطلبه قبل استخدامه.
أي ملف أو مسار غير موجود بهذه القائمة يجب طلبه قبل استخدامه.

4. سكربتات Editor
Namespace الإجباري: Palewick.EditorTools
Assets/Editor/BuildWarningsFixer.cs
Assets/Editor/PreBakeCollisionFixer.cs
Assets/Editor/HorrorLightingTool.cs
Assets/Editor/EnemyAISetupTool.cs
SetupMonsterAnimatorButton.cs: موجود بالمشروع، مساره غير معروف، MenuItem: Tools/Monster/Setup Animator Conditions. مخالف للقواعد (بدون Namespace ويستخدم GameObject.Find). لا تشغّله قبل تصليحه ومعرفة مساره.
أي Editor Tool جديد يحتاج طلباً صريحاً.
لا تعيد إنشاء BuildingNavMeshFixer.cs.
لا تعيد إنشاء NavMeshAgentAutoPlace.cs.
لا تستخدم أداة تضيف Modifiers إلى آلاف المجسمات.
لا تشغّل أي Tool غير موجود بالقائمة بدون تأكيد.

5. البريفابات والشيدرات والموديلات
Player Prefab: Assets/Resources/WhiteclownPlayer.prefab
مكونات اللاعب: PhotonView، PlayerSetup، CharController_Motor، CameraViewSwitcher، PlayerHealth، StaminaSystem، Flashlight.
SSR: Assets/a.last/Flooded_Grounds/PostProcessing/Resources/Shaders/ScreenSpaceReflection.shader
TMP: Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader
TMP يستخدم: #pragma enable_debug_symbols
موديل الزومبي: Assets/Monster/Model/Warzombie F Pedroso.fbx
Rig: Humanoid | Avatar Definition: Create From This Model
Avatar الفرعي: Warzombie F PedrosoAvatar
الملفات الفرعية داخل FBX: mixamo.com، mixamorig:Hips، Warzombie F PedrosoAvatar، WorldWar_zombie (Mesh). ماكو Animation Clips داخل FBX.
التكستشرات: Assets/Monster/Model/world_war_zombie_diffuse، world_war_zombie_normal، world_war_zombie_specular
المادة: Assets/Monster/Model/WorldWar_zombie_material
مجلد أنيميشن الزومبي: Assets/Monster/Animations (لم يُفحص محتواه بعد)
مجلد عام: Assets/Animations (لم يُفحص)
Animator Controller الزومبي: MonsterAnimator (مساره غير مؤكد)
مجلد مؤجل: PostProcessing Runtime/Models (15 ملف ...Model.cs) — نكمله بعدين. تم سابقاً Editor/Models وRuntime/Components.

6. قواعد الرد والعمل
كل الردود باللهجة العراقية.
كل رسالة خطوة واحدة قصيرة ومرقمة.
المستخدم مبتدئ؛ اشرح ضغطة بضغطة.
قبل تعديل سكربت موجود، اطلب نصه الحالي إذا لم يكن متوفراً.
عند إرسال كود، أرسل الملف كاملاً جاهزاً للنسخ.
الكود بدون تعليقات، بدون أسطر فارغة زائدة، وبدون علامات Markdown داخل الملف.
لا تفترض حالة Inspector أو Prefab أو Scene.
الصور تستخدم لفهم Inspector وScene فقط.
أخطاء Console تُرسل كنص منسوخ.
لا تطلب حذف ملفات عشوائياً.
ممنوع حذف Assets/_TerrainAutoUpgrade.
حذف Library أو Temp أو Logs مسموح فقط عند الحاجة وبعد طلب واضح.
لا تغيّر Player Settings أو Quality أو Physics أو Audio أو Camera أو Lighting أو Canvas Anchors بدون طلب صريح.
لا تغيّر Transform أو Scale أو Rotation للماب أو البيوت بدون طلب صريح.
لا تستخدم ملفاً أو مساراً غير مذكور قبل طلبه.
لا تعمل Build بوجود Error.
احفظ كل مسار جديد بالبرومبت أول ما يظهر.

7. إعدادات Photon
Fixed Region: eu | App Version: 1.0 | App Id Chat: فارغ | Protocol: WSS
SendRate: 20 | SerializationRate: 10 (داخل ServerBrowser.Start)
PhotonNetwork.AutomaticallySyncScene = true داخل ServerBrowser.Start قبل دخول أي Room.
قبل ضبط NickName تأكد أن NetworkClientState ليس Disconnecting.
افحص photonView.IsMine داخل Start.
احذف كل Listener وEvent داخل OnDestroy.
كل RpcTarget.All يجب أن يعمل من لاعب واحد إلى أربعة لاعبين.
كل الأجهزة تستخدم نفس نسخة اللعبة.
Master Client فقط يتحكم بحركة وقرارات EnemyAI.

8. قواعد Unity 6.6 API
ممنوع FindObjectsSortMode.
ممنوع FindFirstObjectByType.
استخدم: FindObjectsByType<T>(FindObjectsInactive.Include) و FindAnyObjectByType<T>()
إذا وُجد using System أضف بعده: using Object = UnityEngine.Object;
الخط الافتراضي: Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
للإمبوتر: AssetImporter.GetAtPath(path)
ممنوع Projector.
ممنوع Light.drawHalo.
لا تغيّر إعدادات Pre-Bake بدون فحص PreBakeCollisionFixer.cs الحالي.

9. الحركة والركض
الحركة عبر CharController_Motor.cs
Move Speed: 3.5 | Sprint Speed: 6.5 | Acceleration: 8 | Deceleration: 12 | Gravity: -19.62
المشي والركض عبر SprintButton وStaminaSystem، وPlayerSetup يربطهما باللاعب المحلي Runtime.
ظهور Player Motor أو Stamina بقيمة None قبل التشغيل طبيعي.
الجويستك لا يفعّل الركض تلقائياً.
لا تغيّر قيم الحركة بدون طلب صريح.

10. Animator اللاعب
Parameter: Speed (Float). الحالات: Idle / Walk / Run.
Idle→Walk: Speed > 0.25 | Walk→Idle: Speed < 0.25 | Walk→Run: Speed > 0.75 | Run→Walk: Speed < 0.75
Has Exit Time مطفأ.
CharController_Motor يرسل: Idle = 0 | Walk = 0.5 | Run = 1.
لا تغيّر Animator Controller قبل فحص Parameters والانتقالات.

11. الكاميرا
CameraViewSwitcher.cs: FP Height 1.7 | TP Distance 3.5 | TP Height 1.5 | TP Smooth 15 | Min Pitch -30 | Max Pitch 60 | FP FOV 60 | TP FOV 70 | FOV Smooth 10 | Bob Frequency 1.8 | Bob Horizontal 0.04 | Bob Vertical 0.05 | Bob Speed Threshold 0.5 | Bob Smooth 8.
Is Third Person غير مفعّل افتراضياً.
CameraTurnSfx.cs: تم تصليحه (OnEnable يصفّر القراءة، OnDisable يوقف الصوت، OnDestroy يمسح الـ AudioClip، حماية قسمة صفر). قيم Inspector ما تغيرت.

12. الفلاش والواجهة والصوت والأبواب
FlashlightController محلي فقط وبدون RPC.
Listeners زر ViewSwitchBtn تضيفها PlayerSetup.EnableLocalPlayer فقط. ممنوع أي سكربت آخر يضيفها أو يحذفها.
ممنوع GameObject.Find لعنصر يمكن أن يكون مطفياً. الحقول تُحل Runtime.
الجويستك يستخدم ScreenPointToLocalPointInRectangle مع background.rect.size.
FixCanvasAnchorsTool يتخطى Sliders وJoysticks.
DoorController.cs: تم تصليحه (Awake بدل Start، Camera.main بدل FindGameObjectWithTag، Clamp01 للسرعة، RPC_ToggleDoor بـ AllBuffered). الأبواب بالمشهد بأسماء ..._Door_A_Hinge.
FootstepSoundController.cs: Loop صوت خطوات حسب CharacterController.velocity و isGrounded (لم يُعدّل).

13. PlayerHealth
الملف: Assets/Scripts/PlayerHealth.cs
maxHealth 100 | invincibilityTime 1
TakeDamage(int amount, PhotonMessageInfo info = default) عليه [PunRPC] ويرفض الضرر إذا pv مو IsMine.
الضرر يعمل Offline وOnline، من 1 إلى 4 لاعبين.
Die يعطل CharController_Motor وCharacterController ويرسل RPC_Die لـ Others.
DeathPanel وHealth Slider يحلان Runtime.
مشاكل مكتشفة تنتظر التصليح:
1. RPC_Die وOnPhotonSerializeView يفتحون DeathPanel عند اللاعبين الثانيين لما يموت لاعب غيرهم.
2. UpdateUI للاعب البعيد يغيّر Health Slider المحلي.
3. FindUIElements وBloodEffect ينحلّون حتى للاعب غير المحلي.

14. EnemyAI (النسخة الجديدة المعطاة بهذه الجلسة)
الملف: Assets/Scripts/EnemyAI.cs — MonoBehaviourPunCallbacks + IPunObservable.
بدون دوريات: يثبت بمكان Spawn ويحرس.
يكتشف اللاعب: أقرب من Close Detection Range مباشرة، أو ضمن Detection Range مع خط رؤية (Raycast).
يطارد بدون توقف، يحدّث الوجهة كل 0.1 ثانية، ويهجم واللاعب واقف أو متحرك.
يرجع للـ Spawn إذا: اللاعب أبعد من Lose Target Range، أو الوحش أبعد من Leash Range عن مكانه، أو اللاعب خارج NavMesh (الماء) لمدة 2 ثانية.
الأهداف من PlayerHealth (مو Tag)، ويتخطى اللاعب الميت (Motor أو CharacterController مطفي).
Master Client فقط يحرك؛ الباقين Lerp للموقع والدوران + مزامنة Speed الأنيميشن.
الضرر: RPC_ApplyDamage لكل الأجهزة وصاحب اللاعب فقط ينفّذ PlayerHealth.TakeDamage.
Animator: Speed = 0 Idle / 0.5 Walk (رجوع) / 1 Run (مطاردة)، Trigger Attack وDie إذا موجودين، applyRootMotion = false.
API محفوظ: animator، agent، playerTarget، SetPlayer(Transform)، TakeDamage(float)، Die().
Die: RPC لكل الأجهزة، والمستر يحذفه بـ PhotonNetwork.Destroy بعد 5 ثواني.

15. Monster_AI بالمشهد Scene_A (آخر فحص)
Tag: Enemy | Layer: Default | Static مطفأ
Position: 532.79, 17.5, 187.15 | Rotation Y: 307.46 | Scale 1
PhotonView: ViewID 133، Ownership Fixed، Unreliable On Change، Auto Find All، Observed = EnemyAI
NavMeshAgent: Humanoid، Speed 1.8، Angular 360، Acceleration 12، Stopping 1.6، Auto Braking مطفأ، Radius 0.5، Height 2.2، High Quality، Priority 50، Area Walkable
CapsuleCollider: Is Trigger مفعّل، Center Y 1، Radius 0.5، Height 3
ZombieSoundController موجود
Animator: Controller = MonsterAnimator، Avatar = None (هذا سبب توقف الأنيميشن)، Root Motion مطفأ، Update Normal، Culling Always Animate، Clip Count 4 (Humanoid Muscles 520)
EnemyAI Inspector: Patrol Speed 1.8 | Chase Speed 4.2 | Stop Distance 1.6 | Turn Speed 10 | Detection 15 | Close Detection 5 | Lose Target 25 | Leash 40 | Attack Range 1.15 | Patrol Radius 6 | Eye Height 1.6 | Sight Mask Everything | Max Health 100 | Attack Damage 10 | Attack Cooldown 2.2 | Hit Delay 0.4 | Destination Update 0.1 | Target Scan 0.5 | Unreachable Give Up 2 | NavMesh Sample 1.2 | Animator = Monster_AI | Agent = Monster_AI
الأطفال: Warzombie F Pedroso (mixamorig:Hips، WorldWar_zombie) + PatrolPoints (ما يستخدمه الكود الجديد)

16. NavMesh والبيئة
Agent Type: Humanoid.
NavMeshSurface على الـ Terrain بـ Scene_A، مخبوز، ولير Water مستثنى من Include Layers.

17. المنجز
خَبْز NavMesh وظهور المناطق الزرقاء.
إلغاء Static عن Monster_AI.
إصلاح CS0436 وMenuItem المكرر بـ EnemyAISetupTool.cs داخل Palewick.EditorTools.
تصليح CameraTurnSfx.cs وDoorController.cs.
كتابة EnemyAI.cs الجديد (حراسة + مطاردة مستمرة + رجوع + أونلاين/أوفلاين).
فحص Monster_AI كامل واكتشاف Avatar = None.

18. المعلّق (بالترتيب)
1. ربط Avatar: Animator على Monster_AI ← Avatar = Warzombie F PedrosoAvatar ← Ctrl+S ← تجربة Play.
2. فحص MonsterAnimator: Parameters (Speed Float، Attack Trigger، Die Trigger) والحالات Idle/Walk/Run/Attack والانتقالات.
3. فحص محتوى Assets/Monster/Animations.
4. التأكد من صفر أخطاء Console بعد EnemyAI الجديد.
5. تصليح PlayerHealth (فقرة 13).
6. تصليح SetupMonsterAnimatorButton ومعرفة مساره.
7. PostProcessing Runtime/Models (مؤجل).
ملاحظة: Attack Range = 1.15 صغيرة، وChase Speed 4.2 أقل من ركض اللاعب 6.5 — لا تتغير إلا بطلب.

19. البناء والنسخ الاحتياطي
APK: C:\Users\yf_hdr\Desktop\apkk\Palewick_v1.0.apk
النسخة الاحتياطية: لا توجد بطلب المستخدم.
لا تعمل Build بوجود Error.

20. حالة الجهاز (26/9/2026)
قرص واحد فقط C: بحجم 118 GB، ماكو قرص D.
تنظيف تم: السبات مطفي (powercfg /h off)، مسح .gradle وnpm-cache، إزالة Claude Desktop وNode.js، بقايا copilot وnpm، DISM StartComponentCleanup، مسح SoftwareDistribution\Download، تفريغ السلة.
المساحة الفاضية: من 8.2 GB إلى حوالي 38.7 GB.
برامج يستخدمها المستخدم: Unity 6000.6.1f1، Unity Hub، Brave، Proton VPN، Telegram، VS Code (بدل Visual Studio). Claude من متصفح Brave فقط. Claude يُستخدم من متصفح Brave فقط.
Unity Modules: Android فقط (AndroidPlayer 8.5 GB) + windowsstandalonesupport الأساسي.
مشروع yarekam: 12.16 GB (Library 9.22 GB، Assets 2.93 GB). ملف APK الوحيد: C:\Users\yf_hdr\Desktop\apkk\Palewick_v1.0.apk (0.23 GB).
تحديث: تم مسح Library\Bee (كاش البناء، أول Build جاي أطول) + DISM ResetBase + cleanmgr. المساحة الفاضية: 44.2 GB.
خطة: إزالة Visual Studio Community 2026 وتنصيب VS Code بداله (إضافة Unity من Microsoft + تحديث حزمة Visual Studio Editor + External Script Editor = VS Code).
تحديث: Visual Studio Community 2026 وVisual Studio Installer وملحقاته (.NET SDK، vs_CoreEditorFonts) انشالوا. المساحة الفاضية: 52.6 GB (من 8.2 GB). التالي: تنصيب VS Code وربطه بـ Unity.
تحديث PlayerHealth (جديد، Scripts/PlayerHealth.cs): الواجهة (Slider/DeathPanel/Blood) للاعب المحلي فقط، DeathPanel يُبحث عنه بكل Canvas وبشكل متداخل، RPC_Die/RPC_Revive ما يلمسون UI ولا Motor عند الآخرين، أُضيف public bool IsDead، وRevive يُزامَن.
تحديث EnemyAI: IsValidTarget يعتمد PlayerHealth.IsDead أولاً (لأن Motor ممكن يكون مطفي عند نسخ اللاعبين البعيدين على جهاز المستر).
المحرر: VS Code مربوط بـ Unity (External Script Editor = Visual Studio Code، حزمة Visual Studio Editor v2.0.27). إضافة Unity من Microsoft منصبة.
سكربتات إضافية موجودة بـ Assets/Scripts (ظهرت بـ VS Code، ما انفحصت): GTA6CameraEffects.cs، HeartbeatSfx.cs، IntroManager.cs، MobileMultiTouchInputModule.cs، PauseController.cs
مجلدات إضافية بـ Assets: StreamingAssets، UI_Generated، UI_Icons، Player، Packages، iimage، Flashlight، Animations
.NET SDK 10.0.401 تنصب بـ winget (VS Code يشتغل: Projects Assembly-CSharp، Solution yarekam.slnx).
PlayerHealth الجديد وEnemyAI (IsDead) انلصقوا بالمشروع، والـ Compile نجح (اللعبة اشتغلت بـ Play).
مشكلة جهاز: خطأ D3D11 swapchain device reset/removed (GPU Timeout على Intel) أثناء Play. الحل المطبق: TdrDelay=10 وTdrDdiDelay=20 بالريجستري + Restart. إذا رجع: تحديث تعريف Intel، وسد Brave أثناء Play.
المعلّق التالي: ربط Avatar على Monster_AI ثم تجربة Play.
تحديث EnemyAI (27/9): Avatar انربط والأنيميشن يشتغل. إصلاح الوقفات: TryGetChasePoint (يلحق أقرب نقطة NavMesh حتى لو اللاعب طالع منها لحد offMeshTolerance=2)، loseTargetDelay=3 ثواني قبل ما يترك، CheckStuck كل 0.75 ثانية يعيد المسار، SetDestination بس إذا الهدف تحرك أكثر من 0.3م، وحقل currentStateDebug يبين الحالة بالـ Inspector.

## Status 2026-09-27 (EnemyAI final)
- EnemyAI: chase never stops; returns to spawn only when player > returnDistance (15 m). Fields renamed: chaseSampleDistance=20, stuckTime=0.4, returnDistance=15. Debug fields: currentStateDebug, debugDistance, debugVelocity, debugPath.
- Serialization now sends position, rotation, animSpeed, velocity (all clients need same build).
- Monster_AI PhotonView: ViewID 133, Ownership Fixed, Unreliable On Change, Observed = EnemyAI. Verified.
- APK rebuilt (Succeeded, 0 errors), installed on phone, online test OK per user.

## Status 2026-09-27 (scripts review done)
- Deleted (unused): CameraTurnSfx.cs, MobileMultiTouchInputModule.cs, PauseController.cs, ZombieSoundController.cs (component removed from Monster_AI first), Editor/SetupPubgPauseMenuTool.cs, Editor/EnemyAISetupTool.cs, Editor/SetupZombieHorrorTool.cs.
- Updated: BloodEffectUI, DoorController (state RPC AllViaServer + master sync to late joiners, no buffered), FootstepSoundController (position-delta, remote footsteps), GTA6CameraEffects (additive bob, yaw sway), NetworkManager (autoJoin guard), PlayerQuickChat (all canvases, throttle, cooldown).
- Unchanged OK: HeartbeatSfx, IntroManager, PauseMenuPUBG, PlayerHealth, PlayerInteraction (holds IInteractable), PlayerSetup, SceneLoadingController, ServerBrowser, EnemyAI.
- Note: NetworkManager sets QualitySettings.pixelLightCount = 8 (left as is, needs user request to change).
- Next: rebuild APK (DoorController/Footstep RPC/serialization changes need same build on all devices).

## Status 2026-09-29 (graphics menu art + build)
- Button art: Palewick/UI/Btn_Smooth, Btn_Balanced, Btn_HD, Btn_Ultra, Btn_Ultimate, Btn_Fps30/45/60/90/120/144 (obsidian + gold, text inside, transparent). Imported to Assets/UI_Icons.
- PauseMenuPUBG: field buttonSprites (Sprite[]) on PubgPauseMenu holds all 11; assigned by sprite name at runtime (GfxSpriteNames / "Btn_Fps"+fps). Custom sprites: selected white, others 0.55 gray, preserveAspect, child text hidden.
- Canvas Scaler: Scale With Screen Size 1920x1080, Match = 1 (height). Tested 720p/2160x1080/2960x1440/800x480: OK.
- Editor: Jobs > Burst > Enable Compilation OFF (Burst server dotnet.exe ate 4-5 GB RAM). Build still Burst-AOT.
- Build failed once: Library/BurstCache/JIT/BurstCacheManifest.cm locked. Fix: close Unity, kill dotnet.exe/Unity.exe, delete Library/BurstCache (user approved), rebuild.
- APK rebuilt (Succeeded, 9m35s), phone test: everything works per user.
- PC: Dell Latitude E7240, i5-4310U, 8 GB DDR3 (2/2 slots). Upgrade advice: 2x8 GB DDR3L 1600 SODIMM. Defender exclusions for project + Unity editor, High performance plan, Memory integrity off.

## PLAYER FACTS (never forget) — 2026-09-29
- The player is NOT in Scene_A. It is spawned online by Photon from `Assets/Resources/WhiteclownPlayer.prefab`. Any player change = open this prefab (Project > Resources > WhiteclownPlayer, double-click), never search Scene_A for it.
- Prefab hierarchy: WhiteclownPlayer (Tag Player) > PlayerCamera (Tag MainCamera, Camera + Post-process Layer: Trigger=Player, Layer=PostProcessing, No AA) ; mixamorig:Hips (bones) ; WhiteClown (SkinnedMeshRenderer, Root Bone mixamorig:Hips, material whiteclown_diffuse Standard).
- Root components (order): Transform, Animator, Character Controller, Char Controller_Motor, Player Interaction, Camera View Switcher, Stamina System, Player Setup, Photon View, Photon Transform View, Photon Animator View, Footstep Sound Controller, Player Health, Heartbeat Sfx, Player Quick Chat.
- Flashlight: mixamorig:Hips > Spine > Spine1 > Spine2 > RightShoulder > RightArm > RightForeArm > RightHand > Flashlight (nested prefab "Flashlight": LP_Flash Light mesh, Mesh Renderer, Animator) > Spotlight (nested prefab "Spotlight": Light Spot, Range 30, Spot Angle 80, White, Realtime, Intensity 4, Indirect 1, No Shadows, Cookie set, Draw Halo off).
- FlashlightController.cs (Assets/a.last/Flooded_Grounds/Scripts): SetFlashlight(GameObject), ToggleFlashlight() -> SetActive, icon alpha 1/0.4. Local only, no RPC.
- GTA6CameraEffects (on PlayerCamera) upgraded 2026-09-29: step bob, strafe tilt, breathing, landing dip, sprint FOV kick (fields renamed). User: OK.
- New FlashlightSway.cs (Assets/Scripts) goes on Spotlight inside the prefab: local only (IsMine), aim follows PlayerCamera with lag, shake by speed, rare flicker.

## Status 2026-09-29 (device FPS/RES verified on phone)
- FPS row hides unsupported rates (phone: 30-120 shown, 144 hidden). RES row from Display.main.systemWidth/Height: phone shows 720P/1080P/1220P. Key pw_res (short side), default = highest <= 1080.
- Phone test: SMOOTH + 120 FPS measured 110, RES label 720P after choosing 720P => real. Uniform RES text (SizeResText h*0.6 / w*0.22), Btn_Blank art.
- Texture warnings fixed (palewick_splash_raw, transparent_pixel Compression None). Prebake Collision Meshes already on; remaining 13-mesh collision note is a future-deprecation notice, left as is.
- User declined flashlight shadows / spot angle change.

## PERMANENT RULE (never forget) — 2026-09-29
- The game must work BOTH Online and Offline without any problem.
- Every feature (chat, damage, zombies, doors, health, menus, spawn) must run with no internet and no Photon room, and also in a Photon room with 1-4 players.
- Never call Photon APIs that fail or throw when not connected; guard with PhotonNetwork.InRoom / OfflineMode and fall back to local logic.
- ChatSystem: when not in a room, messages are shown locally.
