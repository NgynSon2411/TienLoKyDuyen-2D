using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TienLo.TrainingView;

namespace TienLo
{
    public sealed class TrainingArena : MonoBehaviour
    {
        private const int StageWidth = 1120;
        private const int StageHeight = 612;
        private const float Ground = TrainingState.Ground;
        private static readonly string[] Phases =
            { "Chuẩn bị", "Tụ lực", "Lấy đà", "Xuất chiêu", "Bùng nổ", "Thu thế" };

        private sealed class TargetView
        {
            public RectTransform Root;
            public RawImage Art;
            public Image Health;
            public Text Status;
        }

        private sealed class Effect
        {
            public RectTransform Rect;
            public Graphic Graphic;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float Duration;
            public bool Particle;
        }

        private readonly TrainingState state = new TrainingState();
        private readonly List<Effect> effects = new List<Effect>();
        private readonly List<Image> boltViews = new List<Image>();
        private static readonly Color PanelColor = Hex("#14243A");
        private static readonly Color LineColor = Hex("#31465B");
        private static readonly Color Copy = Hex("#F3F2E9");
        private TrainingView ui;
        private Character[] characters;
        private RectTransform root, stageFrame, stage, sidebar, controls, effectsLayer, resetControl;
        private RawImage far, near, ground, player;
        private Image shadow, manaFill, cooldownFill;
        private Text title, status, nameLabel, manaLabel, cooldownLabel, hitLabel, damageLabel;
        private Text playerName, phaseLabel;
        private Image[] cards;
        private TargetView[] targets;
        private HoldControl left, right;
        private Button skillButton;
        private int lastWidth, lastHeight;
        private float messageTime;
        private int lastPlayerCharacter = -1;
        private int lastFrame = -1;
        private string lastAnimation;

        private void Awake()
        {
            var catalog = Resources.Load<TextAsset>("TienLo/Catalog");
            characters = JsonUtility.FromJson<CharacterCatalog>(catalog.text).characters;
            ui = new TrainingView();
            Build();
            state.BoltEmitted += (x, character) => Burst(x, Ground - 94, character, 15);
            state.DummyHit += (x, character) =>
            {
                Burst(x, Ground - 125, character, 25);
                AddEffect(x, Ground - 168, character, false);
            };
            Choose(0);
            Layout();
            Render(0);
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
                Layout();

            for (int index = 0; index < characters.Length; index++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + index)) ||
                    Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + index)))
                    Choose(index);
            }
            if (Input.GetKeyDown(KeyCode.K))
                Attack();
            if (Input.GetKeyDown(KeyCode.R))
                ResetArena();

            bool moveLeft = left.Held || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool moveRight = right.Held || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            float dt = Mathf.Min(Time.deltaTime, 0.035f);
            state.Step(dt, (moveRight ? 1 : 0) - (moveLeft ? 1 : 0));
            Render(dt);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused)
                return;
            left?.Clear();
            right?.Clear();
        }

        private void OnApplicationPause(bool paused) => OnApplicationFocus(!paused);

        private void Build()
        {
            var camera = new GameObject("Arena camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(transform, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ink;
            camera.cullingMask = 0;

            var canvasObject = new GameObject("Training UI", typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var background = ui.Panel(canvasObject.transform, "Backdrop", 0, 0, 0, 0, Ink, false);
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
            root = Node(canvasObject.transform, "Page", 0, 0, 1440, 830);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);

            var eventSystem = new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(transform, false);
            eventSystem.GetComponent<EventSystem>().sendNavigationEvents = false;
            title = ui.Label(root, "Tiên Lộ Kỳ Duyên", 28, 11, 800, 52, 39, Gold, true);
            ui.Label(root, "Thanh Vân Sơn  /  Bãi luyện công", 30, 63, 720, 27, 20, Muted);

            BuildStage();
            BuildSidebar();
            BuildControls();
        }

        private void BuildStage()
        {
            stageFrame = ui.Panel(root, "Arena border", 0, 0, 1124, 616, LineColor).rectTransform;
            stage = Node(stageFrame, "Arena viewport", 2, 2, StageWidth, StageHeight);
            stage.gameObject.AddComponent<RectMask2D>();
            ui.Picture(stage, "Moonlit sky", Asset("Sky"), 0, 0, StageWidth, StageHeight);
            far = ui.Picture(stage, "Distant peaks", Asset("Far"), 0, 0, 1500, StageHeight);
            near = ui.Picture(stage, "Mountain ridge", Asset("Near"), 0, 0, 1920, StageHeight);
            ground = ui.Picture(stage, "Training grounds", Asset("Ground"), 0, Ground, StageWidth * 2, 123);
            ground.uvRect = new Rect(0, 0, 2, 1);

            targets = new TargetView[state.Dummies.Length];
            Texture2D dummy = Asset("Dummy");
            for (int index = 0; index < targets.Length; index++)
            {
                RectTransform target = Node(stage, "Training dummy", 0, 0, 112, 250);
                Text label = ui.Label(target, "Hình nộm", 0, 0, 112, 25, 15, Copy, true, TextAnchor.MiddleCenter);
                ui.Panel(target, "Health track", 17, 25, 78, 8, Ink);
                targets[index] = new TargetView
                {
                    Root = target,
                    Status = label,
                    Health = ui.Panel(target, "Health fill", 17, 25, 78, 8, Hex("#F3A76C"), false),
                    Art = ui.Picture(target, "Straw target", dummy, 8, 46, 96, 192)
                };
            }

            shadow = ui.Panel(stage, "Player shadow", 0, 0, 69, 15, new Color(0.02f, 0.08f, 0.1f, 0.5f));
            player = ui.Picture(stage, "Player animation", null, 0, 0, 245, 245);
            playerName = ui.Label(stage, "", 0, 0, 250, 26, 19, Copy, true, TextAnchor.MiddleCenter);
            phaseLabel = ui.Label(stage, "", 0, 0, 250, 23, 16, Gold, false, TextAnchor.MiddleCenter);
            var plaque = ui.Panel(stage, "Location", 20, 20, 223, 44, new Color(0.05f, 0.1f, 0.17f, 0.84f));
            ui.Label(plaque.transform, "SÂN TẬP THANH VÂN", 15, 0, 195, 44, 16, Gold, true);
            effectsLayer = Node(stage, "Skill effects", 0, 0, StageWidth, StageHeight);
        }

        private void BuildSidebar()
        {
            sidebar = ui.Panel(root, "Character panel", 0, 0, 270, 612, PanelColor).rectTransform;
            ui.Label(sidebar, "Chọn nhân vật", 15, 10, 238, 39, 24, Gold, true);
            cards = new Image[characters.Length];
            for (int index = 0; index < cards.Length; index++)
            {
                int selection = index;
                Character character = characters[index];
                var card = ui.Panel(sidebar, character.name, 12 + index % 2 * 126, 55 + index / 2 * 85,
                    120, 78, LineColor);
                cards[index] = card;
                Clickable(card).onClick.AddListener(() => Choose(selection));
                ui.Picture(card.transform, "Portrait", character.Texture("idle", 0), -5, 9, 57, 57);
                ui.Label(card.transform, character.name, 47, 8, 70, 62, 14, Copy, true);
            }
            nameLabel = ui.Label(sidebar, "", 15, 323, 244, 37, 20, Gold, true);
            Bar("Sinh lực", 369, Hex("#EE818A"), out _, out Text health);
            health.text = "100 / 100";
            Bar("Linh lực", 421, Hex("#66C8EF"), out manaFill, out manaLabel);
            Bar("Chiêu thức 1", 473, Hex("#C7A2FF"), out cooldownFill, out cooldownLabel);
            ui.Panel(sidebar, "Divider", 15, 528, 240, 1, LineColor, false);
            hitLabel = ui.Label(sidebar, "", 15, 538, 240, 27, 16, Muted);
            damageLabel = ui.Label(sidebar, "", 15, 570, 240, 27, 16, Muted);
        }

        private void Bar(string caption, int y, Color color, out Image fill, out Text value)
        {
            ui.Label(sidebar, caption, 15, y, 125, 25, 16, Copy);
            value = ui.Label(sidebar, "", 137, y, 118, 25, 15, Muted, false, TextAnchor.MiddleRight);
            ui.Panel(sidebar, caption + " track", 15, y + 29, 240, 8, Ink);
            fill = ui.Panel(sidebar, caption + " fill", 15, y + 29, 240, 8, color, false);
        }

        private void BuildControls()
        {
            controls = Node(root, "Controls", 0, 0, 1124, 105);
            left = MoveButton("Trái", 0);
            right = MoveButton("Phải", 117);
            var skill = ui.Panel(controls, "Skill", 0, 0, 165, 55, Gold);
            skillButton = Clickable(skill);
            skillButton.onClick.AddListener(Attack);
            ui.Label(skill.transform, "Chiêu thức 1", 0, 0, 165, 55, 19, Ink, true, TextAnchor.MiddleCenter);
            var reset = ui.Panel(controls, "Reset", 0, 0, 119, 55, LineColor);
            Clickable(reset).onClick.AddListener(ResetArena);
            ui.Label(reset.transform, "Làm lại", 0, 0, 119, 55, 19, Copy, true, TextAnchor.MiddleCenter);
            status = ui.Label(controls, "", 0, 63, 880, 33, 20, Muted);
            resetControl = reset.rectTransform;
        }

        private HoldControl MoveButton(string caption, int x)
        {
            var surface = ui.Panel(controls, caption, x, 0, 103, 55, LineColor);
            surface.raycastTarget = true;
            ui.Label(surface.transform, caption, 0, 0, 103, 55, 19, Copy, true, TextAnchor.MiddleCenter);
            return surface.gameObject.AddComponent<HoldControl>();
        }

        private void Layout()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            bool narrow = lastWidth < lastHeight * 1.34f;
            float pageWidth = narrow ? 600 : 1460;
            float pageHeight = narrow ? 1055 : 830;
            root.sizeDelta = new Vector2(pageWidth, pageHeight);
            float scale = Mathf.Min(lastWidth / pageWidth, lastHeight / pageHeight);
            root.localScale = new Vector3(scale, scale, 1);

            float stageScale = narrow ? 0.49f : 1;
            float stageDisplayWidth = 1124 * stageScale;
            float stageX = narrow ? (pageWidth - stageDisplayWidth) / 2 : 24;
            const float stageY = 105;
            SetRect(stageFrame, stageX, stageY, 1124, 616);
            stageFrame.localScale = new Vector3(stageScale, stageScale, 1);
            float controlsY = stageY + 616 * stageScale + 14;
            SetRect(controls, stageX, controlsY, stageDisplayWidth, 105);
            SetRect(skillButton.GetComponent<RectTransform>(), stageDisplayWidth - 298, 0, 165, 55);
            SetRect(resetControl, stageDisplayWidth - 119, 0, 119, 55);

            float sidebarWidth = narrow ? stageDisplayWidth : 270;
            SetRect(sidebar, narrow ? stageX : 1162, narrow ? controlsY + 104 : stageY,
                sidebarWidth, narrow ? 518 : 616);
            ArrangeSidebar(narrow, sidebarWidth);
            title.fontSize = narrow ? 38 : 42;
            status.fontSize = narrow ? 15 : 20;
            SetRect(status.rectTransform, 0, 63, stageDisplayWidth, 33);
        }

        private void ArrangeSidebar(bool narrow, float width)
        {
            int columns = narrow ? 3 : 2;
            float columnWidth = narrow ? (width - 24) / columns : 126;
            for (int index = 0; index < cards.Length; index++)
            {
                SetRect(cards[index].rectTransform, 12 + index % columns * columnWidth,
                    55 + index / columns * 85, columnWidth - 6, 78);
                Text caption = cards[index].GetComponentInChildren<Text>();
                SetRect(caption.rectTransform, 47, 8, columnWidth - 55, 62);
                caption.fontSize = narrow ? 17 : 14;
            }

            for (int index = cards.Length + 1; index < sidebar.childCount; index++)
            {
                var child = (RectTransform)sidebar.GetChild(index);
                Vector2 position = child.anchoredPosition;
                if (!sidebarPositions.TryGetValue(child, out float y))
                    sidebarPositions[child] = y = position.y;
                child.anchoredPosition = new Vector2(position.x, y + (narrow ? 85 : 0));
            }
        }

        private readonly Dictionary<RectTransform, float> sidebarPositions = new Dictionary<RectTransform, float>();

        private void Choose(int index)
        {
            state.Choose(index);
            Character character = characters[index];
            nameLabel.text = character.name;
            nameLabel.color = character.Accent;
            for (int i = 0; i < cards.Length; i++)
                cards[i].color = i == index ? Color.Lerp(character.Accent, Ink, 0.64f) : LineColor;
            Message("Đã chọn " + character.name);
        }

        private void Attack()
        {
            if (state.Attack())
                Message("Đang thi triển " + characters[state.Character].spec.ToLowerInvariant());
        }

        private void ResetArena()
        {
            state.Reset();
            left.Clear();
            right.Clear();
            foreach (Effect effect in effects)
                Destroy(effect.Rect.gameObject);
            effects.Clear();
            Message("Bãi luyện công đã được làm mới");
        }

        private void Message(string text)
        {
            status.text = text;
            messageTime = 2.1f;
        }

        private void Render(float dt)
        {
            float camera = state.Camera;
            SetRect(far.rectTransform, -camera * 0.15f, 0, 1500, StageHeight);
            SetRect(near.rectTransform, -camera * 0.33f, 0, 1920, StageHeight);
            SetRect(ground.rectTransform, -Mathf.Repeat(camera, StageWidth), Ground, StageWidth * 2, 123);
            for (int index = 0; index < targets.Length; index++)
            {
                TrainingState.Dummy dummy = state.Dummies[index];
                TargetView target = targets[index];
                SetRect(target.Root, dummy.X - camera - 56, Ground - 227, 112, 250);
                target.Art.color = dummy.Recovery > 0 ? new Color(1, 1, 1, 0.45f) : Color.white;
                target.Status.text = dummy.Recovery > 0 ? "Đang hồi sinh" : "Hình nộm";
                target.Health.rectTransform.sizeDelta = new Vector2(78 * dummy.Health / 100f, 8);
            }

            RenderPlayer(camera);
            RenderEffects(camera, dt);
            manaFill.rectTransform.sizeDelta = new Vector2(240 * state.Mana / 100, 8);
            cooldownFill.rectTransform.sizeDelta = new Vector2(240 * (1 - state.Cooldown), 8);
            manaLabel.text = Mathf.FloorToInt(state.Mana) + " / 100";
            cooldownLabel.text = state.Cooldown > 0 ? state.Cooldown.ToString("0.0") + " s" :
                state.Mana < TrainingState.SkillCost ? "Thiếu linh lực" : "Sẵn sàng";
            hitLabel.text = "Trúng đòn: " + state.Hits;
            damageLabel.text = "Sát thương: " + state.Damage;
            skillButton.interactable = state.CanAttack;
            left.GetComponent<Image>().color = left.Held ? Hex("#486D85") : LineColor;
            right.GetComponent<Image>().color = right.Held ? Hex("#486D85") : LineColor;
            messageTime -= dt;
            if (messageTime <= 0)
                status.text = "A / D  Di chuyển     ·     K  Chiêu thức     ·     1–6  Chọn nhân vật";
        }

        private void RenderPlayer(float camera)
        {
            Character character = characters[state.Character];
            string animation = "idle";
            Frame[] frames = character.idle;
            int index = 0;
            if (state.IsAttacking)
            {
                animation = state.Facing > 0 ? "skillR" : "skillL";
                frames = state.Facing > 0 ? character.skillR : character.skillL;
                index = Mathf.Min(frames.Length - 1,
                    Mathf.FloorToInt(state.AttackTime / TrainingState.SkillDuration * frames.Length));
            }
            else if (state.Movement != 0)
            {
                animation = state.Facing > 0 ? "runR" : "runL";
                frames = state.Facing > 0 ? character.runR : character.runL;
                index = Mathf.FloorToInt(state.RunTime * 10) % frames.Length;
            }

            Frame frame = frames[index];
            if (lastPlayerCharacter != state.Character || animation != lastAnimation || lastFrame != index)
            {
                player.texture = character.Texture(animation, index);
                lastPlayerCharacter = state.Character;
                lastAnimation = animation;
                lastFrame = index;
            }

            float x = state.X - camera;
            float scale = (state.IsAttacking ? 210f : 245f) / frame.w;
            float anchor = state.IsAttacking ? frame.w * 0.4f : (frame.box[0] + frame.box[2]) * 0.5f;
            SetRect(shadow.rectTransform, x - 34, Ground - 3, 69, 15);
            SetRect(player.rectTransform, x - anchor * scale, Ground - 8 - frame.box[3] * scale,
                frame.w * scale, frame.h * scale);
            SetRect(playerName.rectTransform, x - 125, Ground - 270, 250, 26);
            playerName.text = character.name;
            SetRect(phaseLabel.rectTransform, x - 125, Ground - 244, 250, 23);
            phaseLabel.text = state.IsAttacking ? Phases[index] : "";
            phaseLabel.color = character.Accent;
        }

        private void Burst(float x, float y, int character, int count)
        {
            for (int i = 0; i < count; i++)
                AddEffect(x, y, character, true);
        }

        private void AddEffect(float x, float y, int character, bool particle)
        {
            Color color = characters[character].Accent;
            float life = particle ? Random.Range(0.35f, 0.73f) : 0.85f;
            Graphic graphic;
            if (particle)
            {
                float size = Random.Range(2f, 5f);
                graphic = ui.Panel(effectsLayer, "Spark", 0, 0, size, size, color, false);
            }
            else
                graphic = ui.Label(effectsLayer, "−" + TrainingState.HitDamage, 0, 0, 70, 37,
                    27, color, true, TextAnchor.MiddleCenter);

            effects.Add(new Effect
            {
                Rect = graphic.rectTransform,
                Graphic = graphic,
                Particle = particle,
                Position = new Vector2(particle ? x : x - 35, y),
                Velocity = particle ? Random.insideUnitCircle * 240 + Vector2.up * -35 : Vector2.down * 45,
                Life = life,
                Duration = life
            });
        }

        private void RenderEffects(float camera, float dt)
        {
            while (boltViews.Count < state.Bolts.Count)
            {
                var trail = ui.Panel(effectsLayer, "Sword energy", 0, 0, 105, 13, Copy);
                ui.Panel(trail.transform, "Energy core", 36, 4, 66, 5, Copy);
                boltViews.Add(trail);
            }
            for (int i = 0; i < boltViews.Count; i++)
            {
                Image bolt = boltViews[i];
                bolt.gameObject.SetActive(i < state.Bolts.Count);
                if (i >= state.Bolts.Count)
                    continue;
                TrainingState.Bolt projectile = state.Bolts[i];
                bolt.color = characters[projectile.Character].Accent;
                float x = projectile.X - camera - (projectile.Direction > 0 ? 68 : 37);
                SetRect(bolt.rectTransform, x, Ground - 100, 105, 13);
                bolt.rectTransform.localScale = new Vector3(projectile.Direction, 1, 1);
                if (projectile.Direction < 0)
                    bolt.rectTransform.anchoredPosition += Vector2.right * 105;
            }
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                Effect effect = effects[i];
                effect.Life -= dt;
                if (effect.Life <= 0)
                {
                    Destroy(effect.Rect.gameObject);
                    effects.RemoveAt(i);
                    continue;
                }
                effect.Position += effect.Velocity * dt;
                if (effect.Particle)
                    effect.Velocity += Vector2.up * (180 * dt);
                effect.Rect.anchoredPosition = new Vector2(effect.Position.x - camera, -effect.Position.y);
                Color color = effect.Graphic.color;
                color.a = effect.Life / effect.Duration;
                effect.Graphic.color = color;
            }
        }

        private static Texture2D Asset(string key) => Resources.Load<Texture2D>("TienLo/" + key);
    }
}
