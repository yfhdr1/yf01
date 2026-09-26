# SetupMonsterAnimatorButton.cs (النص اللي وصل من المستخدم — المسار غير معروف)
- MenuItem: Tools/Monster/Setup Animator Conditions
- يدور على Monster_AI بـ GameObject.Find، ياخذ Animator Controller، ويتأكد إن الحالات Idle/Walk/Run/Attack موجودة.
- يضبط الانتقالات: Idle>Walk Speed>0.25، Walk>Idle <0.25، Walk>Run >0.75، Run>Walk <0.75، بدون Exit Time، مدة 0.1.
- Any State>Attack بـ Trigger Attack، ومن Attack>Idle بـ Exit Time 0.7.
- محتاج تصليح: namespace Palewick.EditorTools وبدون GameObject.Find.
