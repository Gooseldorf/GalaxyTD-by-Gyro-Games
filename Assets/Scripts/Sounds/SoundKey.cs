
public static class SoundKey
{
    //Single sounds
    public const string Gatling_start = "Gatling_start";
    public const string Gatling_end = "Gatling_end";

    public const string Gatling_muzzleLoop = "Gatling_loop";

    public const string CreepHitPrefics = "CreepHit_";
    public const string CreepDeathPrefics = "CreepDie_";
    public const string Creep_spawn = "creep_spawn";
    public const string Creep_countIterator = "creeps_count";

    public const string TeleportIn = "TeleportIn";
    public const string TeleportOut = "TeleportOut";

    public const string Cell_backToCore = "cell_back";
    public const string Cell_detached = "cell_lost";
    public const string Cell_destroy = "creep_despawn";
    public const string Cell_lowInCore = "core_last_cells";
    public const string Cell_win_count = "cell_win_count";
    public const string Cell_win_full = "cell_win_count_100";

    public const string Core_lost = "core_lost";
    public const string Second_chance = "second_chance";
    public const string Next_wave = "next_wave";

    public const string Typewriter = "interface_dialog";

    public const string Element_activated = "element_activated"; //gate + bridge + conveyor //check for singleat once?
    public const string Element_deactivated = "element_deactivated";//TODO: check this better

    public const string Interface_defeat = "interface_defeat";
    public const string Interface_victory = "interface_victory";
    public const string Interface_button = "interface_button";// if unique sound => change it in instance of ClickableVisualElement
    public const string Interface_pause_on = "interface_button_pause_on";
    public const string Interface_pause_off = "interface_button_pause_off";
    public const string Interface_exitButton = "interface_button_exit";
    public const string Interface_start = "interface_button_start";
    public const string Interface_dialog = "interface_dialog";

    public const string Menu_workshop_ammoSet = "ammo_set";
    public const string Menu_workshop_directiveSet = "directive_set";
    public const string Menu_workshop_barrelSet = "part_barrel";
    public const string Menu_workshop_magazineSet = "part_mag";
    public const string Menu_workshop_recoilSet = "part_recoil";
    public const string Menu_workshop_upgrade = "upgrade_workshop";
    public const string Menu_mission_preview_on = "interface_button_map_creeps_on";
    public const string Menu_mission_preview_off = "interface_button_map_creeps_off";
    public const string Menu_mission_hardmode = "interface_hardmode";
    public const string Menu_mission_dailyButton = "interface_dailyButton";

    public const string Menu_shop_ad_remove = "shop_ad_remove";
    public const string Menu_shop_credits = "shop_credits";
    public const string Menu_shop_crystals = "shop_crystals";
    public const string Menu_shop_crystals_case = "shop_crystals_case";
    public const string Menu_shop_scrap = "shop_scrap";
    public const string Menu_shop_ticket = "shop_ticket";
    public const string Menu_shop_offer = "shop_offer";
    public const string Menu_shop_bundle = "shop_bundle";

    public const string Lacking_ammo = "lacking_ammo";
    public const string Lacking_supplies = "lacking_supplies";

    public const string Tower_manual_reload = "interface_button_manual_reload";
    public const string Tower_build = "tower_set";
    public const string Tower_on = "tower_on";
    public const string Tower_off = "tower_off";
    public const string Tower_upgrade = "tower_upgrade";
    public const string Tower_upgrade5 = "tower_upgrade5";
    public const string Tower_upgrade10 = "tower_upgrade10";
    public const string Tower_upgrade15 = "tower_upgrade15";
    public const string Tower_sell = "tower_remove";
    
    //Playlist contrillers
    public const string MainMenuControllerName = "PlaylistControllerMainMenu";
    public const string LoseGameControllerName = "PlaylistControllerLose";
    public const string WinGameControllerName = "PlaylistControllerWin";
    public const string BattleControllerName = "PlaylistControllerBattle";
    public const string BattleControllerIntenseName = "PlaylistControllerBattleIntense";
    public const string AmbientControllerName = "PlaylistControllerAmbiend";
}
