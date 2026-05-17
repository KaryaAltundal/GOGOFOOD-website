using GOGOFOOD.Models;
using Microsoft.AspNetCore.Identity;

namespace GOGOFOOD.Data
{
    public static class DbInitializer
    {
        public static void Seed(GogoFoodContext context)
        {
            var passwordHasher = new PasswordHasher<User>();

            CreateDemoUserIfNotExists(
                context,
                passwordHasher,
                name: "Demo Admin",
                email: "admin@gogofood.test",
                role: "Admin",
                city: "Ankara",
                district: "Çankaya",
                neighborhood: "Kızılay",
                street: "Atatürk Boulevard",
                addressDetail: "Admin Office",
                latitude: 39.9208,
                longitude: 32.8541
            );

            CreateDemoUserIfNotExists(
                context,
                passwordHasher,
                name: "Demo User",
                email: "user@gogofood.test",
                role: "User",
                city: "Ankara",
                district: "Çankaya",
                neighborhood: "Kavaklıdere",
                street: "Bestekar Caddesi",
                addressDetail: "18",
                latitude: 39.9119,
                longitude: 32.8597
            );

            SeedDemoUsers(context, passwordHasher);

            SeedAsianCaterersAcrossAnkara(context, passwordHasher);
        }

        private static void SeedDemoUsers(
            GogoFoodContext context,
            PasswordHasher<User> passwordHasher)
        {
            var demoUsers = new List<(string Name, string Email, string District, string Neighborhood, string Street, string AddressDetail, double Latitude, double Longitude)>
            {
                ("Ayşe Demir", "user01@gogofood.test", "Çankaya", "Kavaklıdere", "Tunus Caddesi", "No: 12", 39.9095, 32.8591),
                ("Mehmet Kaya", "user02@gogofood.test", "Çankaya", "Bahçelievler", "7. Cadde", "No: 45", 39.9181, 32.8198),
                ("Zeynep Şahin", "user03@gogofood.test", "Çankaya", "Çukurambar", "Muhsin Yazıcıoğlu Caddesi", "No: 18", 39.9048, 32.8096),
                ("Eren Yıldız", "user04@gogofood.test", "Keçiören", "Etlik", "General Dr. Tevfik Sağlam Caddesi", "No: 22", 39.9802, 32.8533),
                ("Elif Arslan", "user05@gogofood.test", "Yenimahalle", "Batıkent", "Atlantis Caddesi", "No: 8", 39.9684, 32.7186),

                ("Can Özkan", "user06@gogofood.test", "Mamak", "Boğaziçi", "Mamak Caddesi", "No: 34", 39.9333, 32.9200),
                ("Derya Aydın", "user07@gogofood.test", "Etimesgut", "Eryaman", "Göksu Caddesi", "No: 16", 39.9868, 32.6312),
                ("Burak Koç", "user08@gogofood.test", "Sincan", "Törekent", "Törekent Caddesi", "No: 51", 39.9710, 32.5845),
                ("Selin Aksoy", "user09@gogofood.test", "Pursaklar", "Merkez", "Ankara Caddesi", "No: 27", 40.0397, 32.8952),
                ("Mert Çelik", "user10@gogofood.test", "Gölbaşı", "Bahçelievler", "Ankara Caddesi", "No: 9", 39.7931, 32.8058),

                ("İrem Kılıç", "user11@gogofood.test", "Altındağ", "Ulus", "Cumhuriyet Caddesi", "No: 14", 39.9430, 32.8575),
                ("Kerem Yılmaz", "user12@gogofood.test", "Polatlı", "Cumhuriyet", "İnönü Caddesi", "No: 33", 39.5842, 32.1472),
                ("Nazlı Kurt", "user13@gogofood.test", "Beypazarı", "İstiklal", "Milli Egemenlik Caddesi", "No: 20", 40.1681, 31.9211),
                ("Onur Bulut", "user14@gogofood.test", "Nallıhan", "Nasuhpaşa", "Ankara Caddesi", "No: 7", 40.1859, 31.3514),
                ("Sude Ergin", "user15@gogofood.test", "Kızılcahamam", "Yenice", "Soğuksu Caddesi", "No: 11", 40.4697, 32.6506),

                ("Alihan Sarı", "user16@gogofood.test", "Kahramankazan", "Atatürk", "Ankara Bulvarı", "No: 25", 40.2056, 32.6813),
                ("Melis Güneş", "user17@gogofood.test", "Çubuk", "Yıldırım Beyazıt", "Atatürk Caddesi", "No: 40", 40.2386, 33.0322),
                ("Tuna Efe", "user18@gogofood.test", "Akyurt", "Beyazıt", "Akyurt Caddesi", "No: 6", 40.1306, 33.0865),
                ("Buse Taş", "user19@gogofood.test", "Elmadağ", "Hasanoğlan", "İstasyon Caddesi", "No: 19", 39.9200, 33.2300),
                ("Deniz Alp", "user20@gogofood.test", "Haymana", "Medrese", "Ankara Caddesi", "No: 13", 39.4320, 32.4970),

                ("Aslı Yüce", "user21@gogofood.test", "Bala", "Merkez", "Bala Caddesi", "No: 10", 39.5530, 33.1230),
                ("Emirhan Polat", "user22@gogofood.test", "Şereflikoçhisar", "İstiklal", "Ankara Caddesi", "No: 30", 38.9450, 33.5380),
                ("Ceren Uslu", "user23@gogofood.test", "Kalecik", "Cuma Saray", "Kalecik Caddesi", "No: 17", 40.0970, 33.4080),
                ("Arda Keskin", "user24@gogofood.test", "Ayaş", "Hacıveli", "Ayaş Yolu", "No: 21", 40.0190, 32.3320),
                ("Yağmur Tan", "user25@gogofood.test", "Güdül", "Yeni", "Cumhuriyet Caddesi", "No: 28", 40.2100, 32.2450),

                ("Kaan Uçar", "user26@gogofood.test", "Çamlıdere", "Merkez", "Çamlıdere Caddesi", "No: 15", 40.4900, 32.4700),
                ("Ece Bayram", "user27@gogofood.test", "Evren", "Merkez", "Atatürk Caddesi", "No: 5", 39.0200, 33.8000),
                ("Bora Ersoy", "user28@gogofood.test", "Çankaya", "Oran", "Panora Caddesi", "No: 32", 39.8530, 32.8560),
                ("Mina Sezer", "user29@gogofood.test", "Çankaya", "Dikmen", "Dikmen Caddesi", "No: 44", 39.8870, 32.8400),
                ("Tolga Işık", "user30@gogofood.test", "Çankaya", "Ayrancı", "Hoşdere Caddesi", "No: 37", 39.8975, 32.8548),

                ("Defne Ekin", "user31@gogofood.test", "Keçiören", "Kuşcağız", "Sanatoryum Caddesi", "No: 24", 39.9960, 32.8610),
                ("Alp Eren", "user32@gogofood.test", "Yenimahalle", "Demetevler", "Vatan Caddesi", "No: 31", 39.9635, 32.7820),
                ("Nisa Kara", "user33@gogofood.test", "Mamak", "Akdere", "Natoyolu Caddesi", "No: 29", 39.9205, 32.9025),
                ("Yiğit Can", "user34@gogofood.test", "Etimesgut", "Bağlıca", "Bağlıca Bulvarı", "No: 26", 39.8932, 32.6336),
                ("Ela Nur", "user35@gogofood.test", "Sincan", "Fatih", "Plevne Caddesi", "No: 23", 39.9630, 32.5790),

                ("Doruk Mete", "user36@gogofood.test", "Pursaklar", "Saray", "Saray Cumhuriyet Caddesi", "No: 35", 40.0550, 32.8890),
                ("İlayda Su", "user37@gogofood.test", "Gölbaşı", "İncek", "İncek Bulvarı", "No: 42", 39.8158, 32.7315),
                ("Baran Tekin", "user38@gogofood.test", "Altındağ", "Hacı Bayram", "Anafartalar Caddesi", "No: 18", 39.9425, 32.8615),
                ("Duru Acar", "user39@gogofood.test", "Polatlı", "Şentepe", "Ankara Caddesi", "No: 38", 39.5905, 32.1500),
                ("Kuzey Demirci", "user40@gogofood.test", "Beypazarı", "Ayvaşık", "Alaaddin Sokak", "No: 4", 40.1695, 31.9230),

                ("Ada Yaman", "user41@gogofood.test", "Nallıhan", "100. Yıl", "Fatih Caddesi", "No: 16", 40.1870, 31.3530),
                ("Rüzgar Korkmaz", "user42@gogofood.test", "Kızılcahamam", "Karşıyaka", "Ankara Caddesi", "No: 41", 40.4720, 32.6485),
                ("Pelin Aslan", "user43@gogofood.test", "Kahramankazan", "Satıkadın", "Kazan Caddesi", "No: 39", 40.2035, 32.6800),
                ("Ege Sönmez", "user44@gogofood.test", "Çubuk", "Cumhuriyet", "Çubuk Caddesi", "No: 12", 40.2400, 33.0300),
                ("Maya Deniz", "user45@gogofood.test", "Akyurt", "Atatürk", "Yıldırım Caddesi", "No: 8", 40.1315, 33.0845),

                ("Atlas Özer", "user46@gogofood.test", "Elmadağ", "Kemalpaşa", "Elmadağ Caddesi", "No: 10", 39.9220, 33.2280),
                ("Lara Koçak", "user47@gogofood.test", "Haymana", "Seyran", "Haymana Caddesi", "No: 17", 39.4340, 32.4990),
                ("Toprak Eren", "user48@gogofood.test", "Bala", "Kartaltepe", "Kartaltepe Caddesi", "No: 9", 39.5550, 33.1210),
                ("Lina Şen", "user49@gogofood.test", "Kalecik", "Halit Cevri Aslangil", "Bağlar Caddesi", "No: 14", 40.0990, 33.4060),
                ("Mira Aydın", "user50@gogofood.test", "Ayaş", "Sinanlı", "Sinanlı Yolu", "No: 6", 40.0210, 32.3340)
            };

            foreach (var demoUser in demoUsers)
            {
                CreateDemoUserIfNotExists(
                    context,
                    passwordHasher,
                    name: demoUser.Name,
                    email: demoUser.Email,
                    role: "User",
                    city: "Ankara",
                    district: demoUser.District,
                    neighborhood: demoUser.Neighborhood,
                    street: demoUser.Street,
                    addressDetail: demoUser.AddressDetail,
                    latitude: demoUser.Latitude,
                    longitude: demoUser.Longitude
                );
            }
        }

        private static void SeedAsianCaterersAcrossAnkara(
            GogoFoodContext context,
            PasswordHasher<User> passwordHasher)
        {
            CreateCatererWithMenus(
                context,
                passwordHasher,
                name: "Japan House",
                email: "caterer@gogofood.test",
                restaurantName: "Japan House",
                district: "Çankaya",
                neighborhood: "Çukurambar",
                street: "Muhsin Yazıcıoğlu Caddesi",
                addressDetail: "Japan House Catering Office",
                latitude: 39.9048,
                longitude: 32.8096,
                theme: "Japanese",
                starterName: "Mini Sushi Starter Selection",
                starterDescription: "Mini sushi bites prepared as an elegant starter selection for corporate catering.",
                starterPrice: 185,
                mainName: "Chicken Teriyaki Bento Set",
                mainDescription: "Teriyaki chicken with steamed rice, seasonal vegetables and Japanese-style side salad.",
                mainPrice: 320,
                dessertName: "Matcha Mochi Dessert Box",
                dessertDescription: "Soft matcha mochi pieces served in individual dessert boxes for guests.",
                dessertPrice: 115,
                drinkName: "Iced Matcha Latte",
                drinkDescription: "Cold matcha latte served as a smooth Japanese-inspired drink option.",
                drinkPrice: 95
            );

            CreateCatererWithMenus(
                context,
                passwordHasher,
                name: "Thai Garden",
                email: "thai@gogofood.test",
                restaurantName: "Thai Garden",
                district: "Keçiören",
                neighborhood: "Etlik",
                street: "General Dr. Tevfik Sağlam Caddesi",
                addressDetail: "Thai Garden Catering Kitchen",
                latitude: 39.9802,
                longitude: 32.8533,
                theme: "Thai",
                starterName: "Thai Spring Roll Cups",
                starterDescription: "Crispy Thai vegetable spring rolls served with sweet chili sauce for event catering.",
                starterPrice: 125,
                mainName: "Pad Thai Catering Bowl",
                mainDescription: "Classic Pad Thai noodles with vegetables, peanuts and Thai-style sauce.",
                mainPrice: 285,
                dessertName: "Mango Sticky Rice Cups",
                dessertDescription: "Sweet sticky rice cups served with mango and coconut sauce.",
                dessertPrice: 130,
                drinkName: "Thai Iced Tea",
                drinkDescription: "Cold Thai tea with milk, served in catering cups.",
                drinkPrice: 90
            );

            CreateCatererWithMenus(
                context,
                passwordHasher,
                name: "Uzbek Sofrası",
                email: "uzbek@gogofood.test",
                restaurantName: "Uzbek Sofrası",
                district: "Mamak",
                neighborhood: "Boğaziçi",
                street: "Mamak Caddesi",
                addressDetail: "Uzbek Sofrası Event Catering",
                latitude: 39.9333,
                longitude: 32.9200,
                theme: "Uzbek",
                starterName: "Samsa Starter Bites",
                starterDescription: "Mini Uzbek samsa pastries prepared for large event service.",
                starterPrice: 105,
                mainName: "Uzbek Plov Catering Tray",
                mainDescription: "Traditional Uzbek rice pilaf with carrots, chickpeas and seasoned meat.",
                mainPrice: 310,
                dessertName: "Chak Chak Dessert Cups",
                dessertDescription: "Sweet honey pastry bites served in individual cups.",
                dessertPrice: 95,
                drinkName: "Ayran with Mint",
                drinkDescription: "Cold ayran drink with a fresh mint option for catering guests.",
                drinkPrice: 60
            );

            CreateCatererWithMenus(
                context,
                passwordHasher,
                name: "China Palace",
                email: "china@gogofood.test",
                restaurantName: "China Palace",
                district: "Yenimahalle",
                neighborhood: "Batıkent",
                street: "Atlantis Caddesi",
                addressDetail: "China Palace Catering Hall",
                latitude: 39.9684,
                longitude: 32.7186,
                theme: "Chinese",
                starterName: "Dim Sum Starter Box",
                starterDescription: "Assorted steamed dim sum pieces served with soy-based dipping sauce.",
                starterPrice: 155,
                mainName: "Sweet & Sour Chicken Tray",
                mainDescription: "Chinese-style sweet and sour chicken with rice and vegetable garnish.",
                mainPrice: 295,
                dessertName: "Sesame Ball Dessert",
                dessertDescription: "Crispy sesame dessert balls with sweet filling.",
                dessertPrice: 105,
                drinkName: "Jasmine Iced Tea",
                drinkDescription: "Refreshing jasmine tea served cold for large group events.",
                drinkPrice: 75
            );

            CreateCatererWithMenus(
                context,
                passwordHasher,
                name: "Hong Kong Bites",
                email: "hongkong@gogofood.test",
                restaurantName: "Hong Kong Bites",
                district: "Etimesgut",
                neighborhood: "Eryaman",
                street: "Göksu Caddesi",
                addressDetail: "Hong Kong Bites Catering Unit",
                latitude: 39.9868,
                longitude: 32.6312,
                theme: "Hong Kong",
                starterName: "Crispy Wonton Cups",
                starterDescription: "Hong Kong-style crispy wontons with light dipping sauce.",
                starterPrice: 135,
                mainName: "Roasted Duck Rice Set",
                mainDescription: "Roasted duck slices with rice, vegetables and Hong Kong-style sauce.",
                mainPrice: 385,
                dessertName: "Egg Tart Mini Set",
                dessertDescription: "Mini Hong Kong egg tarts prepared for event dessert service.",
                dessertPrice: 115,
                drinkName: "Hong Kong Milk Tea",
                drinkDescription: "Classic Hong Kong-style milk tea served cold or warm.",
                drinkPrice: 85
            );

            CreateCatererWithMenus(
                context,
                passwordHasher,
                name: "Seoul Table",
                email: "korean@gogofood.test",
                restaurantName: "Seoul Table",
                district: "Sincan",
                neighborhood: "Törekent",
                street: "Törekent Caddesi",
                addressDetail: "Seoul Table Catering Kitchen",
                latitude: 39.9710,
                longitude: 32.5845,
                theme: "Korean",
                starterName: "Kimchi Pancake Bites",
                starterDescription: "Mini Korean kimchi pancakes served with soy sesame sauce.",
                starterPrice: 120,
                mainName: "Bulgogi Rice Catering Bowl",
                mainDescription: "Korean bulgogi beef with rice, vegetables and sesame garnish.",
                mainPrice: 340,
                dessertName: "Korean Sweet Rice Cakes",
                dessertDescription: "Soft Korean rice cakes served in sweet bite-size portions.",
                dessertPrice: 105,
                drinkName: "Korean Citron Tea",
                drinkDescription: "Fresh citron tea served hot or cold for event guests.",
                drinkPrice: 80
            );

            CreateCatererWithMenus(
                context,
                passwordHasher,
                name: "Saigon Catering",
                email: "vietnam@gogofood.test",
                restaurantName: "Saigon Catering",
                district: "Pursaklar",
                neighborhood: "Merkez",
                street: "Ankara Caddesi",
                addressDetail: "Saigon Catering Office",
                latitude: 40.0397,
                longitude: 32.8952,
                theme: "Vietnamese",
                starterName: "Fresh Summer Roll Set",
                starterDescription: "Vietnamese fresh rolls with vegetables and light dipping sauce.",
                starterPrice: 130,
                mainName: "Vietnamese Noodle Bowl",
                mainDescription: "Rice noodle bowl with vegetables, herbs and Vietnamese-style sauce.",
                mainPrice: 260,
                dessertName: "Coconut Tapioca Cups",
                dessertDescription: "Creamy coconut tapioca dessert cups for catering service.",
                dessertPrice: 95,
                drinkName: "Vietnamese Iced Coffee",
                drinkDescription: "Strong iced coffee with condensed milk, served in event cups.",
                drinkPrice: 90
            );

            CreateCatererWithMenus(
                context,
                passwordHasher,
                name: "Delhi Spice",
                email: "india@gogofood.test",
                restaurantName: "Delhi Spice",
                district: "Gölbaşı",
                neighborhood: "Bahçelievler",
                street: "Ankara Caddesi",
                addressDetail: "Delhi Spice Catering Kitchen",
                latitude: 39.7931,
                longitude: 32.8058,
                theme: "Indian",
                starterName: "Samosa Starter Box",
                starterDescription: "Mini Indian samosas served with mint and tamarind sauces.",
                starterPrice: 115,
                mainName: "Butter Chicken Catering Tray",
                mainDescription: "Creamy butter chicken with basmati rice for event catering.",
                mainPrice: 330,
                dessertName: "Gulab Jamun Dessert Cups",
                dessertDescription: "Sweet gulab jamun pieces served in individual dessert cups.",
                dessertPrice: 100,
                drinkName: "Mango Lassi",
                drinkDescription: "Cold mango yogurt drink served for catering guests.",
                drinkPrice: 80
            );

            CreateCatererWithMenus(
                context,
                passwordHasher,
                name: "Lahore Feast",
                email: "pakistan@gogofood.test",
                restaurantName: "Lahore Feast",
                district: "Polatlı",
                neighborhood: "Cumhuriyet",
                street: "İnönü Caddesi",
                addressDetail: "Lahore Feast Event Kitchen",
                latitude: 39.5842,
                longitude: 32.1472,
                theme: "Pakistani",
                starterName: "Pakora Starter Cups",
                starterDescription: "Crispy vegetable pakoras served with chutney sauce.",
                starterPrice: 105,
                mainName: "Chicken Biryani Catering Tray",
                mainDescription: "Fragrant chicken biryani with rice, spices and event-size serving trays.",
                mainPrice: 295,
                dessertName: "Kheer Dessert Bowls",
                dessertDescription: "Traditional rice pudding dessert served in small catering bowls.",
                dessertPrice: 90,
                drinkName: "Rose Milk",
                drinkDescription: "Cold rose-flavored milk drink for large group service.",
                drinkPrice: 70
            );
        }

        private static User CreateDemoUserIfNotExists(
            GogoFoodContext context,
            PasswordHasher<User> passwordHasher,
            string name,
            string email,
            string role,
            string city,
            string district,
            string neighborhood,
            string street,
            string addressDetail,
            double latitude,
            double longitude)
        {
            var existingUser = context.Users.FirstOrDefault(x => x.Email == email);

            if (existingUser != null)
            {
                existingUser.Name = name;
                existingUser.Role = role;
                existingUser.City = city;
                existingUser.District = district;
                existingUser.Neighborhood = neighborhood;
                existingUser.Street = street;
                existingUser.AddressDetail = addressDetail;
                existingUser.Latitude = latitude;
                existingUser.Longitude = longitude;
                existingUser.IsDeleted = false;

                context.Users.Update(existingUser);
                context.SaveChanges();

                return existingUser;
            }

            var user = new User
            {
                Name = name,
                Email = email,
                Role = role,
                City = city,
                District = district,
                Neighborhood = neighborhood,
                Street = street,
                AddressDetail = addressDetail,
                Latitude = latitude,
                Longitude = longitude,
                IsDeleted = false
            };

            user.Password = passwordHasher.HashPassword(user, "123456");

            context.Users.Add(user);
            context.SaveChanges();

            return user;
        }

        private static void CreateCatererWithMenus(
            GogoFoodContext context,
            PasswordHasher<User> passwordHasher,
            string name,
            string email,
            string restaurantName,
            string district,
            string neighborhood,
            string street,
            string addressDetail,
            double latitude,
            double longitude,
            string theme,
            string starterName,
            string starterDescription,
            decimal starterPrice,
            string mainName,
            string mainDescription,
            decimal mainPrice,
            string dessertName,
            string dessertDescription,
            decimal dessertPrice,
            string drinkName,
            string drinkDescription,
            decimal drinkPrice)
        {
            var user = CreateDemoUserIfNotExists(
                context,
                passwordHasher,
                name: name,
                email: email,
                role: "Caretaker",
                city: "Ankara",
                district: district,
                neighborhood: neighborhood,
                street: street,
                addressDetail: addressDetail,
                latitude: latitude,
                longitude: longitude
            );

            var caretaker = context.Caretakers.FirstOrDefault(x => x.UserId == user.UserId);

            if (caretaker == null)
            {
                caretaker = new Caretaker
                {
                    UserId = user.UserId
                };

                context.Caretakers.Add(caretaker);
            }

            caretaker.Name = name;
            caretaker.RestaurantName = restaurantName;
            caretaker.Address = BuildAddress(addressDetail, street, neighborhood, district);
            caretaker.City = "Ankara";
            caretaker.District = district;
            caretaker.Neighborhood = neighborhood;
            caretaker.Street = street;
            caretaker.AddressDetail = addressDetail;
            caretaker.Latitude = latitude;
            caretaker.Longitude = longitude;
            caretaker.IsDeleted = false;

            context.SaveChanges();

            CreateMenuItemIfNotExists(
                context,
                caretaker.CaretakerId,
                name: starterName,
                category: "Starter",
                price: starterPrice,
                description: starterDescription,
                imageUrl: "",
                customizationOptions: BuildStarterOptions(theme)
            );

            CreateMenuItemIfNotExists(
                context,
                caretaker.CaretakerId,
                name: mainName,
                category: "Main Course",
                price: mainPrice,
                description: mainDescription,
                imageUrl: "",
                customizationOptions: BuildMainCourseOptions(theme)
            );

            CreateMenuItemIfNotExists(
                context,
                caretaker.CaretakerId,
                name: dessertName,
                category: "Dessert",
                price: dessertPrice,
                description: dessertDescription,
                imageUrl: "",
                customizationOptions: BuildDessertOptions(theme)
            );

            CreateMenuItemIfNotExists(
                context,
                caretaker.CaretakerId,
                name: drinkName,
                category: "Drink",
                price: drinkPrice,
                description: drinkDescription,
                imageUrl: "",
                customizationOptions: BuildDrinkOptions(theme)
            );
        }

        private static string BuildAddress(
            string addressDetail,
            string street,
            string neighborhood,
            string district)
        {
            return string.Join(", ", new[]
            {
                addressDetail,
                street,
                neighborhood,
                district,
                "Ankara",
                "Turkey"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static List<(string Name, decimal ExtraPrice)> BuildStarterOptions(string theme)
        {
            return new List<(string Name, decimal ExtraPrice)>
            {
                ("Extra " + theme + " sauce", 12),
                ("Spicy sauce", 15),
                ("Sesame topping", 10),
                ("No salt", 0)
            };
        }

        private static List<(string Name, decimal ExtraPrice)> BuildMainCourseOptions(string theme)
        {
            return new List<(string Name, decimal ExtraPrice)>
            {
                ("Extra " + theme + " sauce", 18),
                ("Extra rice portion", 35),
                ("Extra protein portion", 65),
                ("Make it spicy", 15)
            };
        }

        private static List<(string Name, decimal ExtraPrice)> BuildDessertOptions(string theme)
        {
            return new List<(string Name, decimal ExtraPrice)>
            {
                ("Extra sweet sauce", 12),
                ("Chocolate drizzle", 15),
                ("Fruit topping", 18),
                ("Less sugar", 0)
            };
        }

        private static List<(string Name, decimal ExtraPrice)> BuildDrinkOptions(string theme)
        {
            return new List<(string Name, decimal ExtraPrice)>
            {
                ("Extra ice", 0),
                ("Less sugar", 0),
                ("Mint add-on", 8),
                ("Large cup", 15)
            };
        }

        private static void CreateMenuItemIfNotExists(
            GogoFoodContext context,
            int caretakerId,
            string name,
            string category,
            decimal price,
            string description,
            string imageUrl,
            List<(string Name, decimal ExtraPrice)> customizationOptions)
        {
            var existingMenuItem = context.MenuItems
                .FirstOrDefault(x => x.CaretakerId == caretakerId && x.Name == name);

            if (existingMenuItem == null)
            {
                existingMenuItem = new MenuItem
                {
                    Name = name,
                    Category = category,
                    Price = price,
                    Description = description,
                    ImageUrl = imageUrl ?? string.Empty,
                    IsDeleted = false,
                    CaretakerId = caretakerId
                };

                context.MenuItems.Add(existingMenuItem);
                context.SaveChanges();
            }
            else
            {
                existingMenuItem.Category = category;
                existingMenuItem.Price = price;
                existingMenuItem.Description = description;
                existingMenuItem.IsDeleted = false;

                // Fotoğrafı seed her çalıştığında silmesin.
                // Sadece dolu imageUrl gönderilirse günceller.
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    existingMenuItem.ImageUrl = imageUrl;
                }

                context.MenuItems.Update(existingMenuItem);
                context.SaveChanges();
            }

            foreach (var option in customizationOptions)
            {
                var existingOption = context.CustomizationOptions
                    .FirstOrDefault(x =>
                        x.MenuItemId == existingMenuItem.MenuItemId &&
                        x.Name == option.Name);

                if (existingOption == null)
                {
                    context.CustomizationOptions.Add(new CustomizationOption
                    {
                        Name = option.Name,
                        ExtraPrice = option.ExtraPrice,
                        MenuItemId = existingMenuItem.MenuItemId
                    });
                }
                else
                {
                    existingOption.ExtraPrice = option.ExtraPrice;
                    context.CustomizationOptions.Update(existingOption);
                }
            }

            context.SaveChanges();
        }
    }
}