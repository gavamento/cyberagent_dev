using UnityEngine;

namespace GitTools.EditorTools
{
    /// <summary>Japanese descriptions shared by the Git dashboard and its guide.</summary>
    public static class GitJapanese
    {
        public const string StageTooltip = "この変更を次のコミット対象に選びます。まだ履歴保存もGitHubへの送信も行いません。Unityのアセットは対応する.metaも一緒に確認してください。";
        public const string UnstageTooltip = "コミット対象の選択を解除します。ファイルの編集内容は残ります。";
        public const string CommitTooltip = "選択済みの変更を、このPCのGit履歴に保存します。GitHubへ届けるには、続けて送信（Push）が必要です。";

        public static string Tooltip(string operation)
        {
            switch (operation)
            {
                case "Refresh": return "表示更新（Refresh）：このPCの変更状態と履歴を読み直します。GitHubとの通信やファイルの書き換えは行いません。";
                case "Fetch": return "情報取得（Fetch）：GitHub側の最新履歴とブランチ情報を取得します。作業中のファイルは更新しません。";
                case "Pull": return "取り込み（Pull）：現在のブランチの送信先から最新履歴を取得し、このPCの履歴とファイルへ統合します。共同編集した箇所では競合が起きる場合があります。";
                case "Push": return "送信（Push）：このPCで作成済みの未送信コミットを、追跡先のGitHubブランチへ送ります。未コミットの変更は送信されません。送信先ブランチを先に確認してください。";
                case "Branch": return "ブランチ：現在の履歴から作業用の分岐を作って切り替えます。共有のmainとは別に変更を進めるために使います。";
                case "Stash": return "一時退避（Stash）：まだコミットしていない変更をこのPCへ一時的に預けます。退避一覧から戻せます。GitHubへの送信やバックアップではありません。";
                default: return operation;
            }
        }

        public static string IconSlot(string slot)
        {
            switch (slot)
            {
                case "表示更新": return "Refresh";
                case "情報取得": return "Fetch";
                case "取り込み": return "Pull";
                case "送信": return "Push";
                case "ブランチ": return "Branch";
                case "一時退避": return "Stash";
                case "変更一覧": return "Changes";
                case "タグ": return "Tag";
                default: return slot;
            }
        }

        public static string StatusTooltip(char status)
        {
            switch (status)
            {
                case 'A': return "A：新しく追加されたファイル";
                case 'M': return "M：以前のコミットから内容が変更されたファイル";
                case 'D': return "D：削除されたファイル。コミットすると削除も履歴に記録されます。";
                case 'R': return "R：名前または場所が変わったファイル";
                case 'C': return "C：コピーされたファイル";
                case 'U': return "U：共同編集の競合。内容を確認して解消する必要があります。";
                case 'T': return "T：ファイルの種類が変わっています。";
                default: return "ファイルの状態。詳しい意味は操作ガイドを確認してください。";
            }
        }
    }
}
