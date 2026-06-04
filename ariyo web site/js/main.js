/* =================================================
   Aryo Video Player — Main JS
   ================================================= */

(function () {
    'use strict';

    // =================================================
    // i18n translations
    // =================================================
    const translations = {
        en: {
            'meta.description': 'A modern, fast and elegant video player for Windows. Built with WPF & .NET 9, powered by LibVLC. Subtitle translation, cinema mode, and more.',

            'nav.features': 'Features',
            'nav.screenshots': 'Screenshots',
            'nav.why': 'Why Aryo',
            'nav.download': 'Download',
            'nav.faq': 'FAQ',
            'nav.getApp': 'Get App',

            'hero.badge': 'Version 1.0.0 — Now Available',
            'hero.title1': 'A new way to',
            'hero.title2': 'experience video',
            'hero.title3': 'on Windows.',
            'hero.subtitle': 'Aryo is a premium video player crafted with care. Powered by LibVLC, built with .NET 9 — fast, lightweight, beautiful, and absolutely free.',
            'hero.download': 'Download for Windows',
            'hero.github': 'View on GitHub',
            'hero.meta1': 'Windows 10 / 11',
            'hero.meta2': '64-bit',
            'hero.meta3': '100% Free',
            'hero.meta4': 'No Ads',

            'features.tag': 'Features',
            'features.title': 'Everything you need.<br />Nothing you don\'t.',
            'features.desc': 'A complete set of tools designed to make video playback effortless, immersive, and beautiful.',
            'f1.title': 'Modern UI',
            'f1.desc': 'A clean, dark, distraction-free interface inspired by modern design systems. Looks at home on Windows 11.',
            'f2.title': 'High Performance',
            'f2.desc': 'Hardware-accelerated decoding with LibVLC. Plays 4K HDR content smoothly on modest hardware.',
            'f3.title': 'All Video Formats',
            'f3.desc': 'MP4, AVI, MKV, MOV, WMV, FLV, WebM and more. If VLC can play it, Aryo can play it.',
            'f4.title': 'Smart Playlists',
            'f4.desc': 'Build, sort and save playlists in M3U format. Shuffle, loop and resume where you left off.',
            'f5.title': 'Lightweight',
            'f5.desc': 'No bloat, no telemetry, no background services. Installs in seconds, runs on anything.',
            'f6.title': 'Custom Controls',
            'f6.desc': 'Fine-tune brightness, contrast, saturation, speed (0.25x–3x), aspect ratio and audio delay.',
            'f7.title': 'Hardware Acceleration',
            'f7.desc': 'DXVA2 & NVDEC support for buttery smooth playback. Your GPU does the heavy lifting.',
            'f8.title': 'Subtitle Power',
            'f8.desc': 'SRT, ASS, SUB support with sync, dual subtitles, live translation and audio-to-text via Vosk.',

            'ss.tag': 'Screenshots',
            'ss.title': 'Designed with care.',
            'ss.desc': 'Every pixel is intentional. Take a look at what makes Aryo feel different.',
            'ss.l1': 'Main Player',
            'ss.l2': 'Playlist Manager',
            'ss.l3': 'Subtitle Translation',
            'ss.l4': 'Now Playing',
            'ss.l5': 'Audio to Text',
            'ss.l6': 'Image Controls',

            'why.tag': 'Why Aryo',
            'why.title': 'Built different. <span class="gradient">Built better.</span>',
            'why.desc': 'Other players were designed in 2003. Aryo was designed in 2024.',
            'why.s1l': 'Zero advertisements, ever',
            'why.s2l': 'Lightweight footprint',
            'why.s3l': 'All popular video formats',
            'why.s4l': 'Subtitle translation',
            'cmp.traditional': 'Traditional Players',
            'cmp.r1': 'Modern dark UI',
            'cmp.r2': 'Subtitle translation',
            'cmp.r3': 'Audio to text (offline)',
            'cmp.r4': 'Cinema & night mode',
            'cmp.r5': 'No ads, no telemetry',
            'cmp.r6': 'Hardware acceleration',
            'cmp.r7': 'Modern .NET 9 stack',
            'cmp.r8': 'Free forever',

            'dl.title': 'Ready to experience it?',
            'dl.desc': 'Download Aryo Video Player for Windows. Free, fast, and ready in seconds.',
            'dl.btn': 'Download v1.0.0',
            'dl.src': 'View Source',
            'dl.m1l': 'Platform',
            'dl.m1v': 'Windows 10/11 (64-bit)',
            'dl.m2l': 'Size',
            'dl.m2v': '~120 MB',
            'dl.m3l': 'Framework',
            'dl.m3v': '.NET 9 Desktop Runtime',
            'dl.m4l': 'License',
            'dl.m4v': 'Free for personal use',

            'dev.tag': 'Behind the Code',
            'dev.title': 'Crafted by the AzarCoder team,<br />with obsessive attention to detail.',
            'dev.name': 'AzarCoder Team',
            'dev.role': 'تیم آذر کد · Development Studio',
            'dev.bio': 'A passionate Iranian development team building elegant software with soul. Aryo is named after Ariobarzanes — the Persian commander who stood against Alexander with only 2,500 men.',
            'dev.tech': 'Tech Stack',
            'dev.phil': 'Philosophy',
            'dev.phild': 'Software should feel alive. Every interaction should feel considered. Every pixel should have a purpose. Aryo is not a product — it\'s a piece of craft.',

            'faq.tag': 'FAQ',
            'faq.title': 'Questions? Answered.',
            'faq.desc': 'Everything you need to know about Aryo Video Player.',
            'faq.q1': 'Is Aryo Video Player really free?',
            'faq.a1': 'Yes — 100% free, no hidden costs, no premium tier, no ads, no telemetry. Built as a passion project by a single developer.',
            'faq.q2': 'What video formats are supported?',
            'faq.a2': 'All major formats including MP4, AVI, MKV, MOV, WMV, FLV, WebM. Because Aryo uses LibVLC under the hood, it can play virtually anything VLC can play — including 4K HDR content.',
            'faq.q3': 'Does it support subtitles?',
            'faq.a3': 'Yes! Aryo supports SRT, ASS and SUB subtitle files. It also features live translation to 100+ languages, dual subtitles (original + translation), and an offline audio-to-text engine powered by Vosk.',
            'faq.q4': 'What are the system requirements?',
            'faq.a4': 'Windows 10 or 11 (64-bit), .NET 9 Desktop Runtime (auto-installed), and about 120 MB of disk space. Works on any modern PC.',
            'faq.q5': 'Is the source code available?',
            'faq.a5': 'The binary is free to download. The full source code repository will be available on GitHub soon. Star the repo to be notified.',
            'faq.q6': 'Why is it called "Aryo"?',
            'faq.a6': 'Aryo is named after Ariobarzanes (آریو برزن) — a legendary Persian commander who, with only 2,500 soldiers, held the Persian Gate against Alexander the Great\'s 40,000-strong army. The player is a tribute to that spirit of courage and craft.',
            'faq.q7': 'How do I report a bug or request a feature?',
            'faq.a7': 'Reach out via Telegram at <a href="https://t.me/Thesurenax" target="_blank" rel="noopener">@Thesurenax</a> or open an issue on the GitHub repository. We read everything.',

            'footer.product': 'Product',
            'footer.resources': 'Resources',
            'footer.contact': 'Contact',
            'footer.src': 'Source Code',
            'footer.changelog': 'Changelog',
            'footer.license': 'License',
            'footer.tagline': 'A modern video player for Windows, built with soul.',
            'footer.by': 'A product by',
            'footer.copyright': '© 2024 Aryo Video Player. Crafted with care by AzarCoder Team.',
            'footer.made': 'Made with ♥ in Iran',
        },
        fa: {
            'meta.description': 'یک پلیر ویدیویی مدرن، سریع و زیبا برای ویندوز. ساخته شده با WPF و .NET 9، قدرت گرفته از LibVLC. ترجمه زیرنویس، حالت سینمایی و امکانات بیشتر.',

            'nav.features': 'قابلیت‌ها',
            'nav.screenshots': 'تصاویر',
            'nav.why': 'چرا آریو',
            'nav.download': 'دانلود',
            'nav.faq': 'سوالات',
            'nav.getApp': 'دریافت',

            'hero.badge': 'نسخه ۱.۰.۰ - منتشر شد',
            'hero.title1': 'راهی نوین برای',
            'hero.title2': 'تجربه ویدیو',
            'hero.title3': 'در ویندوز.',
            'hero.subtitle': 'آریو یک پلیر ویدیویی حرفه‌ای است که با دقت ساخته شده. قدرت گرفته از LibVLC، ساخته شده با .NET 9 - سریع، سبک، زیبا و کاملاً رایگان.',
            'hero.download': 'دانلود برای ویندوز',
            'hero.github': 'مشاهده در گیت‌هاب',
            'hero.meta1': 'ویندوز ۱۰ / ۱۱',
            'hero.meta2': '۶۴ بیتی',
            'hero.meta3': '۱۰۰٪ رایگان',
            'hero.meta4': 'بدون تبلیغات',

            'features.tag': 'قابلیت‌ها',
            'features.title': 'همه چیز که نیاز دارید.<br />بدون هیچ اضافه‌ای.',
            'features.desc': 'مجموعه‌ای کامل از ابزارها برای پخش بی‌دردسر، فراگیر و زیبای ویدیو.',
            'f1.title': 'رابط مدرن',
            'f1.desc': 'یک رابط تمیز، تیره و بدون حواس‌پرتی، الهام گرفته از سیستم‌های طراحی مدرن. در ویندوز ۱۱ حس خوبی دارد.',
            'f2.title': 'عملکرد بالا',
            'f2.desc': 'دیکد سخت‌افزاری با LibVLC. محتوای ۴K HDR را حتی روی سیستم‌های معمولی روان پخش می‌کند.',
            'f3.title': 'همه فرمت‌های ویدیو',
            'f3.desc': 'MP4، AVI، MKV، MOV، WMV، FLV، WebM و بیشتر. هر چیزی که VLC پخش کند، آریو هم پخش می‌کند.',
            'f4.title': 'لیست پخش هوشمند',
            'f4.desc': 'ساخت، مرتب‌سازی و ذخیره لیست پخش با فرمت M3U. پخش تصادفی، تکرار و ادامه از جایی که متوقف شدید.',
            'f5.title': 'سبک و کم‌حجم',
            'f5.desc': 'بدون حشو، بدون ردیابی، بدون سرویس پس‌زمینه. در چند ثانیه نصب می‌شود، روی هر چیزی اجرا می‌شود.',
            'f6.title': 'کنترل‌های دقیق',
            'f6.desc': 'تنظیم دقیق روشنایی، کنتراست، اشباع، سرعت (۰.۲۵x تا ۳x)، نسبت تصویر و تاخیر صدا.',
            'f7.title': 'شتاب سخت‌افزاری',
            'f7.desc': 'پشتیبانی DXVA2 و NVDEC برای پخش روان. GPU شما کار سنگین را انجام می‌دهد.',
            'f8.title': 'قدرت زیرنویس',
            'f8.desc': 'پشتیبانی SRT، ASS، SUB با هماهنگ‌سازی، زیرنویس دو زبانه، ترجمه زنده و تبدیل صدا به متن با Vosk.',

            'ss.tag': 'تصاویر',
            'ss.title': 'با دقت طراحی شده.',
            'ss.desc': 'هر پیکسل هدفی دارد. نگاهی بیندازید به چیزی که آریو را متفاوت می‌کند.',
            'ss.l1': 'پلیر اصلی',
            'ss.l2': 'مدیر لیست پخش',
            'ss.l3': 'ترجمه زیرنویس',
            'ss.l4': 'در حال پخش',
            'ss.l5': 'تبدیل صدا به متن',
            'ss.l6': 'کنترل‌های تصویر',

            'why.tag': 'چرا آریو',
            'why.title': 'متفاوت ساخته شده. <span class="gradient">بهتر ساخته شده.</span>',
            'why.desc': 'پلیرهای دیگر در سال ۲۰۰۳ طراحی شدند. آریو در سال ۲۰۲۴ طراحی شده.',
            'why.s1l': 'بدون تبلیغات، همیشه',
            'why.s2l': 'حجم کم',
            'why.s3l': 'همه فرمت‌های محبوب',
            'why.s4l': 'ترجمه زیرنویس',
            'cmp.traditional': 'پلیرهای سنتی',
            'cmp.r1': 'رابط تیره مدرن',
            'cmp.r2': 'ترجمه زیرنویس',
            'cmp.r3': 'تبدیل صدا به متن (آفلاین)',
            'cmp.r4': 'حالت سینمایی و شب',
            'cmp.r5': 'بدون تبلیغ، بدون ردیابی',
            'cmp.r6': 'شتاب سخت‌افزاری',
            'cmp.r7': 'مدرن .NET ۹',
            'cmp.r8': 'رایگان برای همیشه',

            'dl.title': 'آماده تجربه هستید؟',
            'dl.desc': 'آریو ویدیو پلیر را برای ویندوز دانلود کنید. رایگان، سریع و در چند ثانیه آماده.',
            'dl.btn': 'دانلود نسخه ۱.۰.۰',
            'dl.src': 'مشاهده سورس',
            'dl.m1l': 'پلتفرم',
            'dl.m1v': 'ویندوز ۱۰/۱۱ (۶۴ بیت)',
            'dl.m2l': 'حجم',
            'dl.m2v': '~۱۲۰ مگابایت',
            'dl.m3l': 'فریم‌ورک',
            'dl.m3v': 'ران‌تایم دسکتاپ .NET ۹',
            'dl.m4l': 'لایسنس',
            'dl.m4v': 'رایگان برای استفاده شخصی',

            'dev.tag': 'پشت کد',
            'dev.title': 'ساخته شده توسط تیم آذر کد،<br />با دقت وسواس‌گونه به جزئیات.',
            'dev.name': 'تیم آذر کد',
            'dev.role': 'AzarCoder · استودیو توسعه',
            'dev.bio': 'یک تیم توسعه ایرانی پرشور که نرم‌افزارهای ظریف با روح می‌سازد. آریو به یاد آریو برزن نام‌گذاری شده - سردار ایرانی که در برابر اسکندر با تنها ۲۵۰۰ سرباز ایستاد.',
            'dev.tech': 'تکنولوژی‌ها',
            'dev.phil': 'فلسفه',
            'dev.phild': 'نرم‌افزار باید زنده باشد. هر تعامل باید سنجیده باشد. هر پیکسل باید هدفی داشته باشد. آریو یک محصول نیست - یک اثر هنری است.',

            'faq.tag': 'سوالات',
            'faq.title': 'سوالی دارید؟ پاسخ اینجاست.',
            'faq.desc': 'هر آنچه باید درباره آریو ویدیو پلیر بدانید.',
            'faq.q1': 'آیا آریو واقعاً رایگان است؟',
            'faq.a1': 'بله - ۱۰۰٪ رایگان، بدون هزینه پنهان، بدون نسخه پولی، بدون تبلیغات، بدون ردیابی. ساخته شده به عنوان یک پروژه شخصی توسط یک توسعه‌دهنده.',
            'faq.q2': 'چه فرمت‌های ویدیویی پشتیبانی می‌شوند؟',
            'faq.a2': 'همه فرمت‌های اصلی شامل MP4، AVI، MKV، MOV، WMV، FLV، WebM. چون آریو از LibVLC استفاده می‌کند، تقریباً هر چیزی که VLC پخش کند را پخش می‌کند - از جمله محتوای ۴K HDR.',
            'faq.q3': 'آیا زیرنویس پشتیبانی می‌شود؟',
            'faq.a3': 'بله! آریو از فایل‌های زیرنویس SRT، ASS و SUB پشتیبانی می‌کند. همچنین ترجمه زنده به بیش از ۱۰۰ زبان، زیرنویس دو زبانه و موتور آفلاین تبدیل صدا به متن با Vosk دارد.',
            'faq.q4': 'سیستم مورد نیاز چیست؟',
            'faq.a4': 'ویندوز ۱۰ یا ۱۱ (۶۴ بیت)، ران‌تایم دسکتاپ .NET ۹ (نصب خودکار) و حدود ۱۲۰ مگابایت فضای دیسک. روی هر PC مدرنی کار می‌کند.',
            'faq.q5': 'آیا کد منبع در دسترس است؟',
            'faq.a5': 'فایل اجرایی رایگان است. مخزن کد منبع کامل به زودی در گیت‌هاب منتشر می‌شود. مخزن را استار کنید تا مطلع شوید.',
            'faq.q6': 'چرا نام "آریو" است؟',
            'faq.a6': 'آریو به یاد آریو برزن نام‌گذاری شده - سردار افسانه‌ای ایرانی که با تنها ۲۵۰۰ سرباز، دروازه پارس را در برابر ارتش ۴۰ هزار نفری اسکندر مقدونی نگه داشت. این پلیر ادای احترام به آن روح شجاعت و ساخت هنرمندانه است.',
            'faq.q7': 'چطور باگ گزارش دهم یا قابلیت درخواست کنم؟',
            'faq.a7': 'از طریق تلگرام با <a href="https://t.me/Thesurenax" target="_blank" rel="noopener">@Thesurenax</a> یا از طریق issue در مخزن گیت‌هاب با ما در ارتباط باشید. همه چیز را می‌خوانیم.',

            'footer.product': 'محصول',
            'footer.resources': 'منابع',
            'footer.contact': 'تماس',
            'footer.src': 'کد منبع',
            'footer.changelog': 'تغییرات',
            'footer.license': 'لایسنس',
            'footer.tagline': 'یک پلیر ویدیویی مدرن برای ویندوز، ساخته شده با روح.',
            'footer.by': 'ساخته شده توسط',
            'footer.copyright': '© ۲۰۲۴ آریو ویدیو پلیر. ساخته شده با عشق توسط تیم آذر کد.',
            'footer.made': 'ساخته شده با ♥ در ایران',
        }
    };

    let currentLang = localStorage.getItem('aryo-lang') || 'en';

    function applyLanguage(lang) {
        const dict = translations[lang];
        if (!dict) return;

        document.documentElement.setAttribute('lang', lang);
        document.documentElement.setAttribute('dir', lang === 'fa' ? 'rtl' : 'ltr');
        document.documentElement.setAttribute('data-lang', lang);

        document.querySelectorAll('[data-i18n]').forEach(el => {
            const key = el.getAttribute('data-i18n');
            if (dict[key] !== undefined) {
                el.innerHTML = dict[key];
            }
        });

        // Update meta description
        const desc = document.querySelector('meta[name="description"]');
        if (desc && dict['meta.description']) desc.setAttribute('content', dict['meta.description']);

        // Update lang toggle UI
        const activeEl = document.querySelector('.lang-active');
        const otherEl = document.querySelector('.lang-other');
        const langBtn = document.getElementById('langToggle');
        if (activeEl && otherEl) {
            if (lang === 'en') {
                activeEl.textContent = 'EN';
                otherEl.textContent = 'فا';
            } else {
                activeEl.textContent = 'فا';
                otherEl.textContent = 'EN';
            }
        }
        if (langBtn) {
            langBtn.setAttribute('aria-label', lang === 'en' ? 'Switch to Persian' : 'Switch to English');
        }

        // Update page title
        document.title = lang === 'fa'
            ? 'آریو ویدیو پلیر | پلیر ویدیوی حرفه‌ای ویندوز'
            : 'Aryo Video Player — Premium Windows Video Player';

        localStorage.setItem('aryo-lang', lang);
        currentLang = lang;
    }

    // =================================================
    // Loader
    // =================================================
    function hideLoader() {
        const loader = document.getElementById('loader');
        if (loader) {
            setTimeout(() => loader.classList.add('hide'), 600);
        }
    }

    // =================================================
    // Custom cursor + mouse glow
    // =================================================
    function initCursor() {
        if (window.matchMedia('(hover: none)').matches) return;

        const cursor = document.getElementById('cursor');
        const follower = document.getElementById('cursorFollower');
        const glow = document.getElementById('mouseGlow');
        if (!cursor || !follower) return;

        let mouseX = window.innerWidth / 2, mouseY = window.innerHeight / 2;
        let cursorX = mouseX, cursorY = mouseY;
        let followerX = mouseX, followerY = mouseY;
        let glowX = mouseX, glowY = mouseY;

        document.addEventListener('mousemove', e => {
            mouseX = e.clientX;
            mouseY = e.clientY;
            if (glow) glow.classList.add('active');
        });

        document.addEventListener('mouseleave', () => {
            if (glow) glow.classList.remove('active');
        });

        function animate() {
            cursorX += (mouseX - cursorX) * 0.6;
            cursorY += (mouseY - cursorY) * 0.6;
            cursor.style.transform = `translate(${cursorX}px, ${cursorY}px) translate(-50%, -50%)`;

            followerX += (mouseX - followerX) * 0.15;
            followerY += (mouseY - followerY) * 0.15;
            follower.style.transform = `translate(${followerX}px, ${followerY}px) translate(-50%, -50%)`;

            if (glow) {
                glowX += (mouseX - glowX) * 0.08;
                glowY += (mouseY - glowY) * 0.08;
                glow.style.transform = `translate(${glowX}px, ${glowY}px) translate(-50%, -50%)`;
            }
            requestAnimationFrame(animate);
        }
        animate();

        // Hover effect
        const hoverTargets = document.querySelectorAll('a, button, .screenshot-card, summary, .feature-card, .dev-social, .footer-social, .stack-item, .why-stat');
        hoverTargets.forEach(el => {
            el.addEventListener('mouseenter', () => {
                cursor.classList.add('hover');
                follower.classList.add('hover');
            });
            el.addEventListener('mouseleave', () => {
                cursor.classList.remove('hover');
                follower.classList.remove('hover');
            });
        });
    }

    // =================================================
    // Particles
    // =================================================
    function initParticles() {
        const canvas = document.getElementById('particles');
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        let particles = [];
        const PARTICLE_COUNT = 60;
        let w, h;

        function resize() {
            w = canvas.width = window.innerWidth;
            h = canvas.height = window.innerHeight;
        }
        resize();
        window.addEventListener('resize', resize);

        class Particle {
            constructor() { this.reset(true); }
            reset(initial = false) {
                this.x = Math.random() * w;
                this.y = initial ? Math.random() * h : h + 10;
                this.size = Math.random() * 2 + 0.5;
                this.speedY = -(Math.random() * 0.4 + 0.1);
                this.speedX = (Math.random() - 0.5) * 0.3;
                this.opacity = Math.random() * 0.5 + 0.2;
                this.color = ['#19D4F5', '#7C6AFF', '#FF4DC4'][Math.floor(Math.random() * 3)];
            }
            update() {
                this.y += this.speedY;
                this.x += this.speedX;
                if (this.y < -10) this.reset();
            }
            draw() {
                ctx.beginPath();
                ctx.fillStyle = this.color;
                ctx.globalAlpha = this.opacity;
                ctx.shadowBlur = 10;
                ctx.shadowColor = this.color;
                ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
                ctx.fill();
            }
        }

        for (let i = 0; i < PARTICLE_COUNT; i++) particles.push(new Particle());

        function loop() {
            ctx.clearRect(0, 0, w, h);
            ctx.globalAlpha = 1;
            particles.forEach(p => { p.update(); p.draw(); });
            requestAnimationFrame(loop);
        }
        loop();
    }

    // =================================================
    // Navbar scroll
    // =================================================
    function initNav() {
        const nav = document.getElementById('nav');
        const onScroll = () => {
            if (window.scrollY > 20) nav.classList.add('scrolled');
            else nav.classList.remove('scrolled');
        };
        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }

    // =================================================
    // Reveal on scroll
    // =================================================
    function initReveal() {
        const items = document.querySelectorAll('.reveal');
        if (!('IntersectionObserver' in window)) {
            items.forEach(el => el.classList.add('visible'));
            return;
        }
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const delay = parseInt(entry.target.dataset.revealDelay) || 0;
                    setTimeout(() => entry.target.classList.add('visible'), delay);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1, rootMargin: '0px 0px -50px 0px' });
        items.forEach(el => observer.observe(el));
    }

    // =================================================
    // FAQ accordion (close others)
    // =================================================
    function initFaq() {
        const items = document.querySelectorAll('.faq-item');
        items.forEach(item => {
            item.addEventListener('toggle', () => {
                if (item.open) {
                    items.forEach(other => { if (other !== item) other.open = false; });
                }
            });
        });
    }

    // =================================================
    // Lightbox
    // =================================================
    function initLightbox() {
        const lightbox = document.getElementById('lightbox');
        const body = document.getElementById('lightboxBody');
        const close = document.getElementById('lightboxClose');
        const backdrop = lightbox.querySelector('.lightbox-backdrop');
        if (!lightbox) return;

        document.querySelectorAll('.screenshot-card').forEach(card => {
            card.addEventListener('click', () => {
                const content = card.querySelector('.shot-content');
                const title = card.querySelector('.shot-title')?.textContent || 'Aryo Video Player';
                const lightboxTitle = lightbox.querySelector('.lightbox-title');
                if (lightboxTitle) lightboxTitle.textContent = title;

                if (content) {
                    body.innerHTML = '';
                    const clone = content.cloneNode(true);
                    clone.style.width = '100%';
                    clone.style.minHeight = '400px';
                    clone.style.maxHeight = '70vh';
                    clone.style.display = 'flex';
                    clone.style.alignItems = 'center';
                    clone.style.justifyContent = 'center';

                    // If it's an image, ensure proper sizing
                    const img = clone.querySelector('img');
                    if (img) {
                        img.style.width = '100%';
                        img.style.height = 'auto';
                        img.style.maxHeight = '70vh';
                        img.style.objectFit = 'contain';
                    }

                    body.appendChild(clone);
                }
                lightbox.classList.add('open');
                document.body.style.overflow = 'hidden';
            });
        });

        const closeLightbox = () => {
            lightbox.classList.remove('open');
            document.body.style.overflow = '';
        };
        close.addEventListener('click', closeLightbox);
        backdrop.addEventListener('click', closeLightbox);
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape' && lightbox.classList.contains('open')) closeLightbox();
        });
    }

    // =================================================
    // Counter animation for stats
    // =================================================
    function initCounters() {
        const stats = document.querySelectorAll('.why-stat-num');
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const el = entry.target;
                    const text = el.textContent.trim();
                    const match = text.match(/(\d+)/);
                    if (!match) { observer.unobserve(el); return; }
                    const target = parseInt(match[1]);
                    const suffix = text.replace(match[1], '').trim();
                    const prefix = text.split(match[1])[0];
                    let current = 0;
                    const duration = 1500;
                    const start = performance.now();
                    const step = (now) => {
                        const p = Math.min((now - start) / duration, 1);
                        const eased = 1 - Math.pow(1 - p, 3);
                        current = Math.floor(eased * target);
                        el.innerHTML = `${prefix}${current}${suffix ? `<span>${suffix}</span>` : ''}`;
                        if (p < 1) requestAnimationFrame(step);
                        else el.innerHTML = `${prefix}${target}${suffix ? `<span>${suffix}</span>` : ''}`;
                    };
                    requestAnimationFrame(step);
                    observer.unobserve(el);
                }
            });
        }, { threshold: 0.5 });
        stats.forEach(s => observer.observe(s));
    }

    // =================================================
    // Smooth scroll for nav links
    // =================================================
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(link => {
            link.addEventListener('click', e => {
                const id = link.getAttribute('href');
                if (id === '#' || id.length < 2) return;
                const target = document.querySelector(id);
                if (target) {
                    e.preventDefault();
                    const offset = 80;
                    const top = target.getBoundingClientRect().top + window.scrollY - offset;
                    window.scrollTo({ top, behavior: 'smooth' });
                }
            });
        });
    }

    // =================================================
    // Download button — toast notification
    // =================================================
    function initDownload() {
        const btn = document.getElementById('primaryDownload');
        if (btn) {
            btn.addEventListener('click', e => {
                e.preventDefault();
                showToast();
            });
        }
    }

    function showToast() {
        const toast = document.getElementById('toast');
        const title = document.getElementById('toastTitle');
        const desc = document.getElementById('toastDesc');
        const icon = document.getElementById('toastIcon');
        if (!toast) return;

        if (currentLang === 'fa') {
            title.textContent = 'به زودی!';
            desc.innerHTML = 'لینک دانلود به زودی فعال می‌شود. در <a href="https://t.me/Thesurenax" target="_blank" rel="noopener" style="color:var(--cyan);text-decoration:underline">تلگرام</a> دنبال کنید.';
            icon.textContent = '🚧';
        } else {
            title.textContent = 'Coming Soon!';
            desc.innerHTML = 'Download link will be activated soon. Follow us on <a href="https://t.me/Thesurenax" target="_blank" rel="noopener" style="color:var(--cyan);text-decoration:underline">Telegram</a> for updates.';
            icon.textContent = '🚧';
        }

        toast.classList.add('show');
        clearTimeout(toast._timer);
        toast._timer = setTimeout(() => toast.classList.remove('show'), 5000);
    }

    function initToast() {
        const close = document.getElementById('toastClose');
        const toast = document.getElementById('toast');
        if (close && toast) {
            close.addEventListener('click', () => toast.classList.remove('show'));
        }
    }

    // =================================================
    // Back to top button
    // =================================================
    function initBackToTop() {
        const btn = document.getElementById('backToTop');
        if (!btn) return;
        window.addEventListener('scroll', () => {
            if (window.scrollY > 600) btn.classList.add('show');
            else btn.classList.remove('show');
        }, { passive: true });
        btn.addEventListener('click', () => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    // =================================================
    // Scroll progress bar
    // =================================================
    function initScrollProgress() {
        const bar = document.getElementById('scrollProgress');
        if (!bar) return;
        const update = () => {
            const h = document.documentElement.scrollHeight - window.innerHeight;
            const pct = h > 0 ? (window.scrollY / h) * 100 : 0;
            bar.style.width = pct + '%';
        };
        window.addEventListener('scroll', update, { passive: true });
        window.addEventListener('resize', update, { passive: true });
        update();
    }

    // =================================================
    // Language toggle
    // =================================================
    function initLangToggle() {
        const btn = document.getElementById('langToggle');
        if (!btn) return;
        btn.addEventListener('click', () => {
            applyLanguage(currentLang === 'en' ? 'fa' : 'en');
        });
    }

    // =================================================
    // Mobile menu
    // =================================================
    function initMobileMenu() {
        const burger = document.getElementById('navBurger');
        const menu = document.getElementById('navMenu');
        const backdrop = document.getElementById('navBackdrop');
        if (!burger || !menu) return;

        const closeMenu = () => {
            menu.classList.remove('open');
            burger.classList.remove('open');
            if (backdrop) backdrop.classList.remove('open');
            document.body.style.overflow = '';
        };

        const openMenu = () => {
            menu.classList.add('open');
            burger.classList.add('open');
            if (backdrop) backdrop.classList.add('open');
            document.body.style.overflow = 'hidden';
        };

        burger.addEventListener('click', (e) => {
            e.stopPropagation();
            if (menu.classList.contains('open')) closeMenu();
            else openMenu();
        });

        // Close on backdrop click
        if (backdrop) {
            backdrop.addEventListener('click', closeMenu);
        }

        // Close menu when a link is clicked
        menu.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', closeMenu);
        });

        // Close on ESC
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && menu.classList.contains('open')) closeMenu();
        });

        // Close on resize to desktop
        window.addEventListener('resize', () => {
            if (window.innerWidth > 1024 && menu.classList.contains('open')) closeMenu();
        });
    }

    // =================================================
    // Boot
    // =================================================
    document.addEventListener('DOMContentLoaded', () => {
        applyLanguage(currentLang);
        hideLoader();
        initNav();
        initReveal();
        initFaq();
        initLightbox();
        initCounters();
        initSmoothScroll();
        initLangToggle();
        initDownload();
        initMobileMenu();
        initCursor();
        initParticles();
        initToast();
        initBackToTop();
        initScrollProgress();
    });

})();
