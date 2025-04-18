**This project is under heavy development. API and save file compatiblity is not guaranteed.**

This package provides support for building NDMF/Modular Avatar avatars for resonite, including
automatic conversion of VRC physbones.

Currently supported:
* Conversion of VRCSDK viewpoint, viseme, physbone data
* Conversion of skinned and unskinned meshes
* Conversion of textures (including dynamically generated textures)
* Conversion of materials (including dynamic materials)
  * All materials are currently converted to XiexeToonShader

Planned:
* Support for standalone projects (without VRCSDK avatar descriptor)
* Support for attaching resonitepackages to your avatar at build time
* Some kind of support for gestures
* Support for using the unity humanoid bone names instead of relying on resonite heuristics to detect the rig

To get started:
* Install [ALCOM](https://vrc-get.anatawa12.com/en/alcom/). Please do not use the VCC to install this package.
* Add https://vpm.nadena.dev/vpm-reso.json to your VPM sources.
* Install the `Modular Avatar - Resonite Support` package
* Upgrade modular avatar to `1.13.0-alpha.0` or later 
* Enable NDMF_EXPERIMENTAL define:
  * In your unity project: Open Edit -> Project Settings.
  * Select Project -> Player, then open "Other settings".
  * In the "Scripting Define Symbols" field, add `NDMF_EXPERIMENTAL` to the list of defines.
    ![image](readme-assets~/scripting-define.png)
* Open Tools -> NDM Framework -> NDMF Console
* Select your avatar, select Avatar Platform: Resonite, and click Build
* Once "build finished" appears, click "copy to clipboard", then select your resonite window, and press Ctrl-V

Note: AAO and TTT support is pending PRs to those projects.

# 日本語版

**このプロジェクトは開発中です。APIやセーブファイルの互換性は保証されません。**

このパッケージは、Modular AvatarにResonite対応を追加するためのものです。
VRCの揺れもの設定の自動変換などにも対応しています。

セットアップ手順：
* [ALCOM](https://vrc-get.anatawa12.com/ja/alcom/)をインストールします。（VCCを使用しないでください）
* https://vpm.nadena.dev/vpm-reso.json をVPMのソースに追加します。
* `Modular Avatar - Resonite Support`パッケージをインストールします。
* Modular Avatarを`1.13.0-alpha.0`以上にアップグレードします。
* NDMF_EXPERIMENTAL を有効にします：
  * Unityのプロジェクトで、Edit -> Project Settingsを開きます。
  * Project -> Playerを選択し、「Other settings」を開きます。
  * 「Scripting Define Symbols」フィールドに、`NDMF_EXPERIMENTAL`を追加します。
    ![image](readme-assets~/scripting-define.png)
* Tools -> NDM Framework -> NDMF Consoleを開きます。
* アバターを選択し、Avatar Platform: Resoniteを選択してBuildをクリックします。
* 「build finished」と表示されたら、「copy to clipboard」をクリックし、Resoniteのウィンドウを選択してCtrl-Vを押します。

※ AAOやTTTの対応は、PRのマージ待ちです。