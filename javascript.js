function icerigiDegistir() {
    console.log("Butona tıklandı!"); // Çalışıp çalışmadığını konsoldan görmek için

    const baslik = document.getElementById("ana-baslik");
    const metin = document.getElementById("ana-metin");

    if (baslik && metin) {
        baslik.innerText = "2. Hafta: Başlık değişti";
        metin.innerHTML = "İçerik değişti <strong>JavaScript</strong> .";
    } else {
        console.error("Hata: ID'ler bulunamadı. HTML'de id='ana-baslik' ve id='ana-metin' olduğundan emin ol.");
    }
}