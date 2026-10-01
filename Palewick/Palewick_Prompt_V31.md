برومبت Palewick V31 — المرجع الكامل للمشروع
آخر تحديث: 1/10/2026
Unity 6000.6.1f1 | Android | Photon PUN 2 | IL2CPP | ARM64 | LZ4

====================================================================
0. قواعد الرد (إلزامية — اقرأها أول شي)
====================================================================
!!! قاعدة مطلقة: كل رد للمستخدم باللهجة العراقية فقط. ممنوع الرد بالإنكليزي ولا رسالة وحدة. !!!
كل رسالة خطوة واحدة قصيرة ومرقّمة. المستخدم مبتدئ، اشرح ضغطة بضغطة.
عند تسليم ملف: فقط رابط GitHub + المسار داخل المشروع، لا شيء غيره.
ممنوع شرح: افتح Notepad، انسخ، الصق، Ctrl+A، Save As، "ارجع لـ Unity وانتظر التحميل". المستخدم يعرفها.
ممنوع كتابة "شوف الـ Console وأرسل الأخطاء" — المستخدم يخبر بنفسه.
ممنوع كتابة "لما تخلص كول خلصت" بآخر الخطوات — أعطِ الخطوة التالية مباشرة.
الكود يُرسل كملف كامل جاهز للنسخ: بدون تعليقات، بدون أسطر فارغة زائدة، بدون Markdown داخل الملف.
قبل تعديل سكربت موجود اطلب نصه الحالي إذا مو متوفر.
لا تستخدم ملف أو مسار غير مذكور بالبرومبت قبل ما تطلبه.
لا تفترض حالة Inspector أو Prefab أو Scene. الصور تُستخدم لفهم Inspector/Scene فقط.
احفظ كل مسار جديد بالبرومبت أول ما يظهر.
لا نسخ احتياطية إطلاقاً (لا تقترحها ولا تسويها).
لا تعمل Build بوجود Error.
ممنوع حذف Assets/_TerrainAutoUpgrade.
لا تغيّر Player Settings / Quality / Physics / Audio / Camera / Lighting / Canvas Anchors / Transform الماب بدون طلب صريح.
لا تغيّر قيم الحركة بدون طلب صريح.
المستخدم يستخدم VS Code (مو Visual Studio)، وClaude من متصفح Brave فقط.

====================================================================
1. هوية المشروع
====================================================================
اللعبة: Palewick — Horror Multiplayer — Android فقط — Photon PUN 2.
Unity 6000.6.1f1 | IL2CPP | ARM64 | LZ4 | Old Input Manager فقط (New Input System ممنوع نهائياً).
مسار المشروع: C:\Users\yf_hdr\Desktop\yarekam
ناتج البناء الدائم: C:\Users\yf_hdr\Desktop\apkk\Palewick_v1.0.apk (كل Build يستبدل نفس الملف).
مشروع Unity Cloud: Palewick (المؤسسة نفس الاسم). الحساب: yousif6889.

====================================================================
2. ترتيب المشاهد
====================================================================
Assets/a.loby/Scene_Intro  →  Assets/a.loby/Scene_Lobby  →  Assets/a.last/Flooded_Grounds/Scenes/Scene_A

====================================================================
3. سكربتات Runtime (Assets/Scripts إلا المذكور)
====================================================================
AutoRunButton.cs | BloodEffectUI.cs | ChatSystem.cs | DeathScreen.cs | DoorController.cs | EnemyAI.cs
FlashlightSway.cs | FootstepSoundController.cs | GTA6CameraEffects.cs | HeartbeatSfx.cs | IntroManager.cs
JumpButton.cs | LoadingScreenFx.cs | LobbyManager.cs | Minimap.cs | NetworkManager.cs | PauseMenuPUBG.cs
PlayerHealth.cs | PlayerInteraction.cs | PlayerSetup.cs | PointPickup.cs | PwAds.cs | PwAuthUI.cs
PwCloud.cs | PwGoogle.cs | PwLanguageUI.cs | PwLocalizer.cs | PwPoints.cs | PwPointsHud.cs | PwStartMode.cs
ServerBrowser.cs
Assets/a.last/Flooded_Grounds/Scripts/: FlashlightController.cs | StaminaSystem.cs | CameraViewSwitcher.cs | FPSController/CharController_Motor.cs
PwRtl: كلاس static داخل PauseMenuPUBG.cs (PwRtl.Visual لتشكيل العربي/الكردي).
IInteractable: واجهة معرّفة داخل PlayerInteraction.cs — أي Pickup أو باب ينفّذها.

====================================================================
4. أدوات Editor (Assets/Editor) — Namespace إجباري: Palewick.EditorTools
====================================================================
LobbyBuilder.cs ................ Palewick/Build Horror Lobby (يمسح الكانفس ويبني اللوبي + ينادي PwLoginBuilder)
GameScreensBuilder.cs .......... Palewick/Build Horror Game Screens (داخل DeathPanel + LoadingPanel)
IntroBuilder.cs ................ Palewick/Build Horror Intro
ChatBuilder.cs ................. Palewick/Create Chat In Canvas
MinimapBuilder.cs .............. بناء المينيماب
HudCornerLock.cs ............... Palewick/Lock HUD To Screen Corners (يتخطى PointsBadge)
PwLoginBuilder.cs .............. Palewick/Build Login Screen (شاشة اللغة + الدخول + سؤال أونلاين/أوفلاين + شارة النقاط)
PwPointsBuilder.cs ............. Palewick/Build Points HUD + Palewick/Create Point Pickup
PwServicesDefines.cs ........... Palewick/Refresh Service Defines (يضيف PW_ADMOB / PW_GPGS تلقائياً)
PwPluginMetaFixer.cs ........... Palewick/Fix Plugin Meta Files (يرقّي ملفات .meta القديمة للبلَكنات)
BuildWarningsFixer.cs | PreBakeCollisionFixer.cs | HorrorLightingTool.cs | DoorSetupTool.cs | MapCollisionTool.cs
MarkStaticTool.cs | NavMeshBakerTool.cs | PhotonCrashPreventer.cs | ToggleNavMeshTool.cs
أي Editor Tool جديد يحتاج طلباً صريحاً.

====================================================================
5. اللاعب (حقائق ثابتة)
====================================================================
اللاعب مو موجود بـ Scene_A — Photon يولّده من Assets/Resources/WhiteclownPlayer.prefab.
أي تعديل على اللاعب = فتح هذا البريفاب، ممنوع تدوّر عليه بالمشهد.
الهرم: WhiteclownPlayer (Tag Player) > PlayerCamera (Tag MainCamera + Post-process Layer) ؛ mixamorig:Hips ؛ WhiteClown (SkinnedMeshRenderer).
مكونات الجذر: Transform, Animator, CharacterController, CharController_Motor, PlayerInteraction, CameraViewSwitcher,
StaminaSystem, PlayerSetup, PhotonView, PhotonTransformView, PhotonAnimatorView, FootstepSoundController,
PlayerHealth, HeartbeatSfx, PlayerQuickChat.
الفلاش: ...RightHand > Flashlight > Spotlight (FlashlightSway على Spotlight، محلي فقط).

====================================================================
6. Photon
====================================================================
Region: eu | App Version: 1.0 | Protocol: WSS | SendRate 20 | SerializationRate 10 (داخل ServerBrowser.Start)
PhotonNetwork.AutomaticallySyncScene = true قبل دخول أي Room.
Master Client فقط يتحكم بحركة وقرارات EnemyAI.
كل RpcTarget.All لازم يشتغل من 1 إلى 4 لاعبين.
قبل ضبط NickName تأكد أن الحالة مو Disconnecting/Leaving.

====================================================================
7. قواعد Unity 6.6 API
====================================================================
ممنوع FindObjectsSortMode | ممنوع FindFirstObjectByType.
استخدم FindObjectsByType<T>(FindObjectsInactive.Include) و FindAnyObjectByType<T>().
ممنوع GameObject.Find لعنصر ممكن يكون مطفي؛ الحقول تُحل Runtime.
إذا وُجد using System أضف بعده: using Object = UnityEngine.Object;
الخط الافتراضي: Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
ممنوع Projector | ممنوع Light.drawHalo.
ممنوع AppDomain.GetAssemblies داخل كود Editor (تحذير UAC0005) — استخدم CompilationPipeline أو فحص المجلدات.

====================================================================
8. قواعد دائمة (لا تتغير)
====================================================================
• اللعبة لازم تشتغل Online و Offline بدون أي مشكلة. كل ميزة تشتغل بلا إنترنت وبلا Photon room، وكذلك داخل room بـ 1–4 لاعبين.
  ممنوع استدعاء Photon APIs تفشل وقت عدم الاتصال؛ احمِ بـ PhotonNetwork.InRoom / OfflineMode.
• الواجهة لازم تكون موجودة بالإيديتور داخل الكانفس (ظاهرة وقابلة للتعديل بالـ Hierarchy/Scene)، مو مبنية Runtime فقط.
• كل أيقونة PNG جديدة: Texture Type = Sprite (2D and UI)، Sprite Mode = Single، ثم Apply — اذكرها للمستخدم كل مرة.
• أزرار HUD: دائرية وبنفس ستايل الرعب المعتمد (مولّد Palewick/Tools/hud_button_style.py، أيقونات MDI/FA مجانية، ممنوع فن PUBG).
• HUD لازم يكون نفسه بكل الأجهزة: العناصر مربوطة بأقرب زاوية شاشة وبحجم ثابت (Palewick/Lock HUD To Screen Corners).
• تبويبات الإعدادات الجانبية دائماً على اليمين بكل اللغات (محتوى الصفحات بس ينعكس للعربي/الكردي).
• Canvas Scaler: Scale With Screen Size 1920x1080، Match = 1 (Height).

====================================================================
9. نظام النقاط + Unity Cloud + AdMob  (مكتمل وشغّال)
====================================================================
الحزم: com.unity.services.authentication + com.unity.services.cloudsave (منصّبة).
مزوّد الدخول المفعّل بلوحة Unity: Username & Password (Enabled). Google Play Games مؤجل لحد Play Console.

PwCloud.cs
  - تهيئة UnityServices + ResumeAsync (يستخدم الجلسة المخزونة فقط، ممنوع أي Anonymous).
  - SignInEmailAsync: الإيميل يتحول ليوزرنيم Unity (الإيميل نفسه إذا ≤20 حرف صالح، وإلا "pw" + 18 hex من SHA256).
  - الباسوورد: 8–30 حرف مع حرف كبير وصغير ورقم ورمز.
  - SignInGoogleAsync: كود GPGS → SignInWithGooglePlayGamesAsync (يحتاج PW_GPGS).
  - LoadIntAsync / SaveIntAsync على Cloud Save Data.Player.
  - مفاتيح: pw_account, pw_account_type.

PwPoints.cs
  - مفتاح Cloud Save: "points". لاعب جديد = 3 نقاط (StartPoints). RespawnCost = 1.
  - كاش محلي: pw_points_<playerId> + دلتا معلّقة pw_pending_<playerId>.
  - أوفلاين: التغييرات تنحفظ بالـ pending وتنرفع للسيرفر أول ما يرجع الاتصال (cloud + pending، إعادة محاولة كل 6 ثواني).
  - API: Points, Synced, CanRespawn, Add(int), TrySpend(int), event Changed(int).

PointPickup.cs
  - أوبجكت بالمشهد: PhotonView + SphereCollider (Trigger) + Visual + Glow.
  - يُلقط بالمرور فوقه (autoPickup) أو بزر التفاعل (IInteractable).
  - RPC_Take(actorNumber) بـ RpcTarget.AllBufferedViaServer → الغرض ينلقط مرة وحدة بالغرفة، والنقطة للاعب اللي طالبها أول.
  - أوفلاين ينطي النقطة محلياً.

PwAds.cs (AdMob Rewarded)
  - يُجمَّع فقط عند تعريف PW_ADMOB.
  - UseTestAd = true → إعلان Google التجريبي ca-app-pub-3940256099942544/5224354917
  - AndroidRewardedId (الحقيقي) = ca-app-pub-9194615148813735/8226703242
  - App ID (بإعدادات البلَكن) = ca-app-pub-9194615148813735~9623545392
  - API: Available / Ready / Preload() / Show(Action<bool>) — يستخدم MobileAdsEventExecutor (ممنوع RaiseAdEventsOnUnityMainThread).
  - الإعلان ما يظهر داخل الإيديتور أبداً، لازم APK على الموبايل.

DeathScreen.cs
  - Respawn يصرف نقطة وحدة (ترجع إذا فشل الإحياء) ويتعطل إذا النقاط < 1.
  - زر Watch Ad: الإعلان ينطي +1 ثم يحيي اللاعب (صافي 0).
  - يعرض عداد النقاط + سطر "إعادة الظهور تكلف نقطة وحدة" + سطر رسائل (Not enough points / Ad not ready).

الواجهات المبنية بالإيديتور
  - اللوبي (Palewick/Build Login Screen): LanguagePanel, LoginPanel, StartModePanel, PointsBadge, AccountBar.
  - Scene_A (Palewick/Build Points HUD): PointsBadge بأعلى الوسط (مستثنى من HudCornerLock).
  - Palewick/Create Point Pickup: ينشئ Pickup بمكان كاميرا المشهد (مادة Assets/UI_Lobby/PointPickupMat.mat).

حدود معروفة: النقاط يكتبها الكلاينت؛ للحماية من الغش لاحقاً نحتاج Cloud Code أو تحقق من السيرفر.

====================================================================
10. شاشة اللغة + الدخول + أونلاين/أوفلاين (2026-10-01)
====================================================================
PwLanguageUI.cs
  - شاشة كاملة أول تشغيل بس: کوردی / العربية / English.
  - تخزن pw_lang (0=إنكليزي، 1=عربي، 2=كردي) + pw_lang_set=1، وما تظهر بعدها أبداً.
  - أسماء اللغات تُكتب Runtime بخط Resources/Fonts/UniMahanBilal مع PwRtl.Visual.
  - PwLocalizer يتخطى كل شي داخل LanguagePanel.

PwAuthUI.cs
  - شاشة الدخول تظهر فقط إذا ماكو حساب مخزون بالجهاز (pw_account فارغ).
  - بعد أول دخول: الجلسة تنحفظ وتدخل اللوبي مباشرة.
  - بدون إنترنت + حساب مخزون: تدخل مباشرة بنقاطك المخزونة وتتزامن لاحقاً (pw_offline_ok).
  - أزرار: Sign In / Sign Up / Sign in with Google / Play Offline (تبين فقط عند الحاجة) / Sign Out.

PwStartMode.cs
  - زر START باللوبي يفتح نافذة: Play Online أو Play Offline.
  - Online → LobbyManager.OnStartPressed | Offline → LobbyManager.OnSinglePlayerPressed.
  - إذا ماكو اتصال Photon يتعطل زر الأونلاين ويطلع سطر "No internet connection".
  - PwLoginBuilder يفك ربط زر START القديم ويربطه بـ PwStartMode.Open تلقائياً.

====================================================================
11. قائمة الإعدادات (PauseMenuPUBG.cs) — إصلاح التخطيط 2026-10-01
====================================================================
المشكلة القديمة: القائمة كانت مبنية بنِسَب مئوية (الشريط الجانبي 0.83–1، الصفحات 0.02–0.81) فتختلف الأحجام والأماكن حسب نسبة أبعاد كل جهاز.
الحل المطبق:
  - إطار ثابت: FrameW = 1480، FrameH = 1020، بوسط الكانفس (PwRoot) مع خلفية Grunge.
  - SideW = 340 (الشريط الجانبي ثابت على اليمين)، PadX = 24.
  - التبويبات والصفحات والشريط العلوي والسفلي كلها بإزاحات بكسل ثابتة من حواف الإطار.
  - FitFrame(): إذا الشاشة أضيق من الإطار (تابلت 4:3) ينصغّر الإطار كله بنفس النسبة — نفس الشكل بالضبط مو متمطط. ينتحدّث كل 0.5 ثانية بالـ Update.
النتيجة: نفس الأحجام ونفس الأماكن بكل الأجهزة، والتبويبات تظل يمين بكل اللغات.

====================================================================
12. PlayerHealth / EnemyAI / Monster_AI
====================================================================
PlayerHealth: maxHealth 100، invincibilityTime 1، IsDead، Revive() مزامَن.
  الواجهة (Slider/DeathPanel/Blood) للاعب المحلي فقط. RPC_Die/RPC_Revive ما يلمسون UI عند الآخرين.
EnemyAI: حالات Idle, Patrol, Investigate, Search, Chase, Return.
  السمع: ركض 16م، قفز 10م، مشي 4.5م، أبواب 12م (EnemyAI.HearNoise).
  يفقد الهدف بعد 3 ثواني بلا رؤية → يفحص آخر مكان → يبحث 5 ثواني → يرجع.
  يفتح الأبواب المغلقة أمامه (DoorController.OpenFrom). ضوء الفلاش عليه ضمن 9م/22° يبطّئ المطاردة x0.7.
  المستر فقط يحرّك؛ الباقي Lerp. الضرر عبر RPC_ApplyDamage.
Monster_AI بالمشهد: ViewID 133، Ownership Fixed، Observed = EnemyAI، Avatar = Warzombie F PedrosoAvatar.

====================================================================
13. الحركة والكاميرا (ممنوع تغييرها بدون طلب)
====================================================================
CharController_Motor: Move 3.5 | Sprint 6.5 | Accel 8 | Decel 12 | Gravity -19.62
Animator اللاعب: Speed (Float) — Idle 0 / Walk 0.5 / Run 1، عتبات 0.25 و 0.75، Has Exit Time مطفأ.
CameraViewSwitcher: FP Height 1.7 | TP Distance 3.5 | TP Height 1.5 | FP FOV 60 | TP FOV 70 | Pitch -30..60.

====================================================================
14. المنجز بهذه الجلسة (30/9 – 1/10/2026)
====================================================================
• نظام النقاط كامل: دخول بالإيميل عبر Unity Authentication، النقاط بـ Cloud Save، 3 نقاط للاعب الجديد،
  +1 لكل غرض بالخريطة، -1 للرجعة بعد الموت، وزر إعلان AdMob يرجّع اللاعب عايش.
• حساب AdMob + تطبيق Palewick + وحدة Rewarded، والبلَكن v11.5.0 منصّب والإعلان التجريبي اشتغل على الموبايل.
• مزوّد Username & Password مفعّل بلوحة Unity Cloud.
• شاشة اختيار اللغة لمرة وحدة، وشاشة الدخول ما تتكرر، وسؤال أونلاين/أوفلاين على زر البدء.
• إصلاح تخطيط قائمة الإعدادات ليكون ثابت بكل الأجهزة.
• تنظيف التحذيرات: أداة Fix Plugin Meta Files + حذف Photon/PhotonUnityNetworking/Demos فقط.
  (تنبيه: المستخدم مسح مرة فولدر PhotonUnityNetworking كامل بالغلط وانرجع من سلة المحذوفات — امسح Demos بس.)

====================================================================
15. المعلّق
====================================================================
1. إكمال معلومات الدفع بحساب AdMob حتى تجي إعلانات حقيقية.
2. عند الرفع على Play: بدّل UseTestAd إلى false بملف Assets/Scripts/PwAds.cs.
3. الدخول بـ Google: يحتاج Google Play Console + Play Games Services + Web Client ID، بعدها بلَكن GPGS v11
   والـ define PW_GPGS ينضاف تلقائياً، وتضاف Google Play Games كـ Identity Provider بلوحة Unity.
4. حماية من الغش: التحقق من النقاط عبر Cloud Code بدل الكلاينت.
5. تجربة أونلاين بلاعبين (حسابين مختلفين) للتأكد أن الغرض ينلقط مرة وحدة.
6. PostProcessing Runtime/Models (مؤجل).

====================================================================
16. حالة الجهاز
====================================================================
Dell Latitude E7240، i5-4310U، 8 GB DDR3، قرص C: فقط 118 GB (المساحة الفاضية صارت ~52 GB بعد التنظيف).
Jobs > Burst > Enable Compilation مطفي بالإيديتور (كان ياكل RAM)، والبناء يظل Burst-AOT.
إذا فشل البناء بسبب Library/BurstCache: سكّر Unity، اقتل dotnet.exe و Unity.exe، امسح Library/BurstCache، أعد البناء.
