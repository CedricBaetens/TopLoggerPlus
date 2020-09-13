using System;
using System.Collections.Generic;
using System.Text;

namespace TopLoggerPlus.ApiWrapper.Model
{
    public class Route
    {
        public int id { get; set; }
        public string number { get; set; }
        public string grade { get; set; }
        public int wall_id { get; set; }
        public string position_x { get; set; }
        public string position_y { get; set; }
        public string climb_type { get; set; }
        public bool suitable_for_kids { get; set; }
        public int gym_id { get; set; }
        public int setter_id { get; set; }
        public int hold_id { get; set; }
        public bool live { get; set; }
        public bool lived { get; set; }
        public bool deleted { get; set; }
        public DateTime date_live_start { get; set; }
        public DateTime date_set { get; set; }
        public DateTime created_at { get; set; }
        public int nr_of_ascends { get; set; }
        public string average_opinion { get; set; }
        public bool auto_grade { get; set; }
        public string grade_stability { get; set; }
        public string grade_stability_admin { get; set; }
        public int zones { get; set; }
        public bool renew { get; set; }
        public string remarks { get; set; }
        public int rope_number { get; set; }
        public DateTime date_removed { get; set; }
    }
}
