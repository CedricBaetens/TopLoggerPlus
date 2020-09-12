using System;
using System.Collections.Generic;
using System.Text;

namespace TopLoggerPlus.Logic
{
    public class Gym
    {
        public int id { get; set; }
        public string id_name { get; set; }
        public string slug { get; set; }
        public string name { get; set; }
        public string name_short { get; set; }
        public string gym_type { get; set; }
        public bool has_routes { get; set; }
        public bool live { get; set; }
        public int floorplan_version { get; set; }
        public bool edit_climb_show_number { get; set; }
        public bool edit_climb_show_name { get; set; }
        public bool edit_climb_show_remarks { get; set; }
        public bool edit_climb_show_setter { get; set; }
        public bool edit_climb_show_position { get; set; }
        public bool edit_climb_show_expected_removal_at { get; set; }
        public bool vote_renewal { get; set; }
        public bool report_btn { get; set; }
        public bool rope_numbers { get; set; }
        public bool show_repeat_btns { get; set; }
        public bool boulders_grouped_by_wall { get; set; }
        public bool routes_grouped_by_wall { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string postal_code { get; set; }
        public string url_website { get; set; }
        public string phone_number { get; set; }
        public Opening_Hours opening_hours { get; set; }
        public int nr_of_climbs { get; set; }
        public int nr_of_routes { get; set; }
        public int nr_of_boulders { get; set; }
        public int nr_of_users { get; set; }
        public string scale_collapse_climbs { get; set; }
        public string scale_collapse_walls { get; set; }
        public bool ask_community_grade { get; set; }
        public bool show_for_kids { get; set; }
        public int label_new_days { get; set; }
        public int label_removed_days { get; set; }
        public string local_device_pwd { get; set; }
        public string remarks_quick_add { get; set; }
        public bool tablets_on_manual { get; set; }
        public bool tablets_on { get; set; }
        public string grading_system_routes { get; set; }
        public string grading_system_boulders { get; set; }
        public List<Grade_Distribution_Routes> grade_distribution_routes { get; set; }
        public List<Grade_Distribution_Boulders> grade_distribution_boulders { get; set; }
        public bool auto_grade { get; set; }
        public int auto_grade_stable_votes { get; set; }
        public bool show_grade_stability_admin { get; set; }
        public bool show_zones { get; set; }
        public bool show_setter { get; set; }
        public bool renew { get; set; }
        public bool show_setter_popularity { get; set; }
        public bool reservations_enabled { get; set; }
        public bool reservations_guest_enabled { get; set; }
        public int reservations_spots_per_booking { get; set; }
        public string revervation_settings_json { get; set; }
        public int reservations_book_advance_days { get; set; }
        public int reservations_overbooking_count { get; set; }
        public int reservations_cancel_advance_hours { get; set; }
        public string time_zone { get; set; }
        public string serializer { get; set; }
        public List<Hold> holds { get; set; }
        public List<Setter> setters { get; set; }
        public List<Wall> walls { get; set; }
        public List<Gym_Resources> gym_resources { get; set; }
    }

    public class Opening_Hours
    {
        public int version { get; set; }
        public Items items { get; set; }
        public Days days { get; set; }
    }

    public class Items
    {
        public string item1 { get; set; }
        public string item2 { get; set; }
        public string item3 { get; set; }
        public string item4 { get; set; }
    }

    public class Days
    {
        public Monday Monday { get; set; }
        public Tuesday Tuesday { get; set; }
        public Wednesday Wednesday { get; set; }
        public Thursday Thursday { get; set; }
        public Friday Friday { get; set; }
        public Saturday Saturday { get; set; }
        public Sunday Sunday { get; set; }
    }

    public class Monday
    {
        public bool closed { get; set; }
        public Items1 items { get; set; }
    }

    public class Items1
    {
        public string item1 { get; set; }
        public string item2 { get; set; }
        public string item3 { get; set; }
        public string item4 { get; set; }
    }

    public class Tuesday
    {
        public bool closed { get; set; }
        public Items2 items { get; set; }
    }

    public class Items2
    {
        public string item1 { get; set; }
        public string item2 { get; set; }
        public string item3 { get; set; }
        public string item4 { get; set; }
    }

    public class Wednesday
    {
        public bool closed { get; set; }
        public Items3 items { get; set; }
    }

    public class Items3
    {
        public string item1 { get; set; }
        public string item2 { get; set; }
        public string item3 { get; set; }
        public string item4 { get; set; }
    }

    public class Thursday
    {
        public bool closed { get; set; }
        public Items4 items { get; set; }
    }

    public class Items4
    {
        public string item1 { get; set; }
        public string item2 { get; set; }
        public string item3 { get; set; }
        public string item4 { get; set; }
    }

    public class Friday
    {
        public bool closed { get; set; }
        public Items5 items { get; set; }
    }

    public class Items5
    {
        public string item1 { get; set; }
        public string item2 { get; set; }
        public string item3 { get; set; }
        public string item4 { get; set; }
    }

    public class Saturday
    {
        public bool closed { get; set; }
        public Items6 items { get; set; }
    }

    public class Items6
    {
        public string item1 { get; set; }
        public string item2 { get; set; }
        public string item3 { get; set; }
        public string item4 { get; set; }
    }

    public class Sunday
    {
        public bool closed { get; set; }
        public Items7 items { get; set; }
    }

    public class Items7
    {
        public string item1 { get; set; }
        public string item2 { get; set; }
        public string item3 { get; set; }
        public string item4 { get; set; }
    }

    public class Grade_Distribution_Routes
    {
        public string value { get; set; }
        public int count { get; set; }
    }

    public class Grade_Distribution_Boulders
    {
        public string value { get; set; }
        public int count { get; set; }
    }

    public class Hold
    {
        public int id { get; set; }
        public int gym_id { get; set; }
        public string color { get; set; }
        public string brand { get; set; }
    }

    public class Setter
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int gym_id { get; set; }
        public string email { get; set; }
        public bool linked { get; set; }
        public bool is_setter { get; set; }
        public bool admin { get; set; }
        public string name { get; set; }
        public int order { get; set; }
    }

    public class Wall
    {
        public int id { get; set; }
        public int gym_id { get; set; }
        public string name { get; set; }
        public string floorplan_id { get; set; }
        public int order { get; set; }
        public string label_x { get; set; }
        public string label_y { get; set; }
    }

    public class Gym_Resources
    {
        public int id { get; set; }
        public int gym_id { get; set; }
        public string resource_type { get; set; }
        public string url { get; set; }
        public int order { get; set; }
    }

}
