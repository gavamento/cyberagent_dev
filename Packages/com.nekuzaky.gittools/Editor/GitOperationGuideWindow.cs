using UnityEditor;
using UnityEngine;

namespace GitTools.EditorTools
{
    /// <summary>Read-only Japanese guidance for the actual commands exposed by this package.</summary>
    public sealed class GitOperationGuideWindow : EditorWindow
    {
        const float MinimumWidth = 600f;
        const float MinimumHeight = 480f;
        const float SectionSpacing = 12f;
        const float CardSpacing = 5f;
        const float RefreshButtonWidth = 112f;
        const int BodyFontSize = 13;

        sealed class OperationDescription
        {
            public readonly string Title;
            public readonly string ChangesWhere;
            public readonly string Meaning;

            public OperationDescription(string title, string changesWhere, string meaning)
            {
                Title = title;
                ChangesWhere = changesWhere;
                Meaning = meaning;
            }
        }

        static readonly OperationDescription[] BasicOperations =
        {
            new OperationDescription("表示更新（Refresh）", "このPCの状態を読み直す", "変更ファイルや履歴の表示を更新します。GitHubとの通信や作業ファイルの書き換えは行いません。"),
            new OperationDescription("情報取得（Fetch）", "GitHub → このPCの参照情報", "GitHub側の最新履歴とブランチ情報を取得します。作業ファイルには取り込みません。GitHubに変更があるかを確認するための操作です。"),
            new OperationDescription("取り込み（Pull）", "GitHub → このPCの履歴とファイル", "現在のブランチの追跡先から履歴を取得し、このPCの履歴とファイルへ統合します。このツールはマージ方式で取り込みます。同じ箇所を共同編集していると競合の解消が必要です。"),
            new OperationDescription("＋で選択（Stage）", "このPCのコミット対象", "次のコミットへ入れる変更を選びます。選んだ時点の内容が対象です。選択後にさらに編集した場合は、追加の変更も選ぶ必要があります。まだ履歴保存も送信も行いません。"),
            new OperationDescription("−で選択解除（Unstage）", "このPCのコミット対象", "コミットに入れる選択を解除します。ファイルの編集内容は残ります。"),
            new OperationDescription("コミット（Commit）", "このPCのGit履歴", "選択済みの変更と説明文を、このPCの履歴に記録します。未選択の変更は入りません。この時点ではGitHubへ届いていません。コミットは、最後にコミットした状態からの差分をまとめた履歴です。"),
            new OperationDescription("送信（Push）", "このPCのコミット → GitHub", "追跡先のGitHubブランチへ、まだ送っていないコミットを送信します。未コミットのファイル変更は送信されません。他の人の更新で拒否された場合は、情報取得と取り込みを行って内容を確認します。"),
            new OperationDescription("ブランチ作成・切り替え", "このPCの作業場所とファイル", "ブランチは履歴の分岐です。作成すると現在の履歴から作業用ブランチへ切り替わります。GitHub側に新しいブランチを作るには、そのブランチで送信（Push）します。切り替えではファイルの状態も変わります。"),
            new OperationDescription("コミット＋送信", "このPCの履歴 → GitHub", "コミットの保存が成功した後に、送信（Push）を続けて実行します。送信だけ失敗した場合も、成功したコミットはこのPCに残ります。同じ変更をもう一度コミットする必要はありません。")
        };

        static readonly OperationDescription[] AdvancedOperations =
        {
            new OperationDescription("統合（Merge）", "このPCの現在のブランチ", "別ブランチの変更を現在のブランチへまとめます。競合があれば内容を確認して解消します。GitHubへ届けるには、その後に送信が必要です。"),
            new OperationDescription("一時退避（Stash）", "このPCだけ", "未コミットの変更を一時的に預けます。通常は追跡済みファイルが対象で、新しいファイルを含める選択肢もあります。適用すると編集へ戻り、popでは成功後に退避一覧からも外れます。GitHubへのバックアップではありません。"),
            new OperationDescription("直前修正（Amend）", "このPCの直前のコミット", "直前のコミットを新しい内容で作り直します。コミットIDも変わります。すでに共有したコミットに使うと、他の人の履歴と食い違うため、チームと確認せず使用しないでください。"),
            new OperationDescription("変更を打ち消す（Revert）", "このPCに新しいコミット", "選んだコミットの変更を打ち消す、新しいコミットを作ります。元の履歴は残ります。GitHubへ反映するには、その後に送信します。"),
            new OperationDescription("この変更だけ取り込む（Cherry-pick）", "このPCの現在のブランチ", "別の履歴から選んだコミットの変更を、現在のブランチへ適用して新しいコミットにします。共同編集と重なると競合が発生する場合があります。"),
            new OperationDescription("履歴を戻してファイルを残す（Reset mixed）", "このPCの履歴と選択状態", "ブランチの先端を選んだコミットまで戻し、コミット対象の選択を解除します。それ以降のファイルの変更は、未コミットの変更として残ります。GitHub側の履歴は、この操作だけでは変わりません。"),
            new OperationDescription("履歴とファイルを戻す（Reset hard）", "このPCの履歴と追跡済みファイル", "選んだコミットの状態へ戻し、追跡済みファイルの未コミット変更を破棄します。まだGitに保存していない編集は復元できない場合があります。通常の送受信のために使う操作ではありません。"),
            new OperationDescription("×で変更を破棄（Discard）", "このPCのファイル", "そのファイルの未コミット変更を取り消します。新しいファイルの場合は削除します。コミットしていない編集が必要なら、この操作を実行しないでください。"),
            new OperationDescription("競合を解消済みにする", "このPCのコミット対象", "競合したファイルの内容を先に修正してから実行します。Gitに解消済みと伝え、コミット対象へ選びます。ボタンを押すだけで正しい内容に修正されるわけではありません。"),
            new OperationDescription("タグ（Tag）", "履歴の目印", "特定のコミットにバージョン名などの目印を付けます。GitHub側へ届けるには、タグの送信操作が別途必要です。"),
            new OperationDescription("ブランチの削除", "このPC、またはGitHub側", "このPCのブランチ削除と、GitHub側のブランチ削除は別です。GitHub側の削除は共同作業者にも影響します。ブランチ名を確認し、未統合の履歴を失わないようにします。"),
            new OperationDescription("コミットの状態を見る（detached HEAD）", "このPCの作業場所", "ブランチから離れて過去のファイル状態を確認します。そのまま作業を続ける前に、新しい作業ブランチを作成してください。")
        };

        Vector2 m_Scroll;
        GitStatus m_Status = new GitStatus();
        string m_StatusError;
        bool m_IsLoading;
        bool m_ShowAdvanced;
        GUIStyle m_BodyStyle;
        GUIStyle m_HeadingStyle;

        [MenuItem("Tools/Git 操作ガイド")]
        public static void Open()
        {
            var window = GetWindow<GitOperationGuideWindow>("Git 操作ガイド");
            window.minSize = new Vector2(MinimumWidth, MinimumHeight);
            window.Show();
        }

        void OnEnable()
        {
            RefreshStatus();
        }

        void RefreshStatus()
        {
            if (m_IsLoading) return;
            if (!GitRunner.IsRepository)
            {
                m_StatusError = "このプロジェクトにはGitの管理情報がありません。";
                return;
            }
            m_IsLoading = true;
            GitRunner.RunAsync("status --porcelain=v1 -b", result =>
            {
                if (this == null) return;
                m_IsLoading = false;
                m_StatusError = result.Ok ? null : result.Combined;
                if (result.Ok) m_Status = GitStatus.Parse(result.Output);
                Repaint();
            });
        }

        void EnsureStyles()
        {
            if (m_BodyStyle == null)
                m_BodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = BodyFontSize, richText = false };
            if (m_HeadingStyle == null)
                m_HeadingStyle = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };
        }

        void OnGUI()
        {
            EnsureStyles();
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            GUILayout.Label("Gitで何が、どこに保存される？", m_HeadingStyle);
            EditorGUILayout.HelpBox("編集・保存 → ＋でコミット対象を選択 → コミット → 送信（Push）\nコミットはこのPCの履歴、送信はGitHubへの共有です。", MessageType.Info);

            GUILayout.Space(SectionSpacing);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("このプロジェクトの状態", m_HeadingStyle);
                using (new EditorGUI.DisabledScope(m_IsLoading))
                    if (GUILayout.Button(m_IsLoading ? "読み取り中…" : "状態を読み直す", GUILayout.Width(RefreshButtonWidth))) RefreshStatus();
            }
            EditorGUILayout.LabelField("保存場所", GitRunner.RepositoryRoot);
            if (!string.IsNullOrEmpty(m_StatusError)) EditorGUILayout.HelpBox(m_StatusError, MessageType.Warning);
            else
            {
                EditorGUILayout.LabelField("現在のブランチ", m_Status.Branch);
                EditorGUILayout.LabelField("追跡先", string.IsNullOrEmpty(m_Status.Upstream) ? "まだ設定されていません" : m_Status.Upstream);
                GUILayout.Label("未送信コミット：" + m_Status.Ahead + " ／ 未取得コミット：" + m_Status.Behind + " ／ 変更ファイル：" + m_Status.Changes.Count, m_BodyStyle);
                GUILayout.Label("この件数は最後に情報取得した時点の参照情報との比較です。最新のGitHub状態を調べるには、Git画面の「情報取得」を使います。", EditorStyles.wordWrappedMiniLabel);
                if (m_Status.HasConflicts) EditorGUILayout.HelpBox("競合があります。先に内容を確認して解消し、コミットしてから送信します。", MessageType.Warning);
                if (m_Status.Detached) EditorGUILayout.HelpBox("現在はブランチから離れた状態です。作業を続ける前に作業ブランチを作成してください。", MessageType.Warning);
                if (m_Status.Branch == "main" || m_Status.Branch == "master")
                    EditorGUILayout.HelpBox("共有の基本ブランチにいます。共同作業では、チームで決めた自分の作業ブランチに切り替えてから変更を進めます。", MessageType.Info);
            }

            GUILayout.Space(SectionSpacing);
            GUILayout.Label("普段の作業の流れ", m_HeadingStyle);
            GUILayout.Label("1. Unityのシーン・Prefab・スクリプトをファイルへ保存します。\n2. 作業前に「情報取得」で相手の更新を確認し、必要なら「取り込み」を行います。\n3. チーム指定の作業ブランチを確認します。\n4. 変更一覧で差分を読み、今回入れるファイルだけ＋で選びます。\n5. 何を変えたかが分かる説明文を入力して「コミット」します。\n6. ブランチと履歴を確認し、「送信」でGitHubへ届けます。", m_BodyStyle);
            EditorGUILayout.HelpBox("Unityの.metaは、画像・Prefabなどのアセットを識別するためのファイルです。対応するアセットと一緒に確認します。Library・Temp・Logsなどの自動生成物は通常送信しません。ProjectSettingsの変更も、今回必要な内容か確認して選びます。", MessageType.Info);
            GUILayout.Label("例：「プロジェクト追加」と入力してコミットすると、その説明が履歴に付きます。コメント欄へ入力するだけでは、保存も送信も行われません。", m_BodyStyle);

            GUILayout.Space(SectionSpacing);
            GUILayout.Label("基本操作の意味", m_HeadingStyle);
            foreach (var operation in BasicOperations) DrawOperation(operation);

            GUILayout.Space(SectionSpacing);
            m_ShowAdvanced = EditorGUILayout.Foldout(m_ShowAdvanced, "取り消し・統合などの詳しい操作", true);
            if (m_ShowAdvanced)
                foreach (var operation in AdvancedOperations) DrawOperation(operation);

            GUILayout.Space(SectionSpacing);
            GUILayout.Label("ファイル横の記号", m_HeadingStyle);
            GUILayout.Label("A＝追加　M＝変更　D＝削除　R＝名前変更　U＝競合\n＋＝コミット対象へ選択　−＝選択を解除　×＝編集を破棄", m_BodyStyle);
            GUILayout.Space(SectionSpacing);
            GUILayout.Label("送信できないとき", m_HeadingStyle);
            GUILayout.Label("・コミット前の変更だけがある：先に必要な変更を選択し、コミットします。\n・相手の履歴が先に進んでいる：情報取得 → 内容を確認 → 取り込み → 競合があれば解消 → 再送信。\n・Authentication／Permission／403：GitHubのログイン、アカウントの権限、ブランチの保護ルールを確認します。\n・作者名やメールが未設定：このプロジェクトで使うコミット作者情報を設定します。\n・Unity Cloudの接続エラー：GitHubとは接続先が別です。Gitの実行記録と区別して確認します。", m_BodyStyle);
            EditorGUILayout.HelpBox("Gitの実行記録、差分の原文、ブランチ名、作者名、過去のコミット文は原文で表示します。Git自身が出す英語のエラーは、内容をそのまま確認できるように残しています。", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        void DrawOperation(OperationDescription operation)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(operation.Title, m_HeadingStyle);
                GUILayout.Label("変わる場所：" + operation.ChangesWhere, EditorStyles.wordWrappedMiniLabel);
                GUILayout.Label(operation.Meaning, m_BodyStyle);
            }
            GUILayout.Space(CardSpacing);
        }
    }
}
