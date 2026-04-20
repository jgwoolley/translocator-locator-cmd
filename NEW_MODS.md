# Adding new mods

These are the project references that you should add:

```xml
<ItemGroup>
    <Reference Include="VintagestoryAPI">
        <HintPath>$(VINTAGE_STORY)/VintagestoryAPI.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="VSSurvivalMod">
        <HintPath>$(VINTAGE_STORY)/Mods/VSSurvivalMod.dll</HintPath>
        <Private>False</Private>
    </Reference>
    <Reference Include="VSEssentials">
        <HintPath>$(VINTAGE_STORY)/Mods/VSEssentials.dll</HintPath>
        <Private>False</Private>
    </Reference>
    <Reference Include="VSCreativeMod">
        <HintPath>$(VINTAGE_STORY)/Mods/VSCreativeMod.dll</HintPath>
        <Private>False</Private>
    </Reference>
    <Reference Include="Newtonsoft.Json">
        <HintPath>$(VINTAGE_STORY)/Lib/Newtonsoft.Json.dll</HintPath>
        <Private>False</Private>
    </Reference>
    <Reference Include="0Harmony">
        <HintPath>$(VINTAGE_STORY)/Lib/0Harmony.dll</HintPath>
        <Private>False</Private>
    </Reference>
    <Reference Include="VintagestoryLib">
        <HintPath>$(VINTAGE_STORY)/VintagestoryLib.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="protobuf-net">
        <HintPath>$(VINTAGE_STORY)/Lib/protobuf-net.dll</HintPath>
        <Private>False</Private>
    </Reference>
    <Reference Include="cairo-sharp">
        <HintPath>$(VINTAGE_STORY)/Lib/cairo-sharp.dll</HintPath>
        <Private>False</Private>
    </Reference>
    <Reference Include="Microsoft.Data.Sqlite">
        <HintPath>$(VINTAGE_STORY)/Lib/Microsoft.Data.Sqlite.dll</HintPath>
        <Private>False</Private>
    </Reference>
</ItemGroup>
```

But then in Rider remove the unused ones: `Open Project Node / Dependencies / Right Click / Refactor This / Remove Unused References`