const cleanWikiPage = () => {
    const badClasses = ["div.top-ads-container", "div.ad-slot-placeholder", "div.notifications-placeholder", "aside.page__right-rail"]

    const badIds = ["incontent-player", "WikiaBar", "featured-video__player-container"]

    const removeAds = () => {
        for (let badClass of badClasses) {
            document.querySelectorAll(badClass).forEach(el => el.remove());
        }

        for (let badId of badIds) {
            const elem = document.getElementById(badId);
            if (elem) elem.remove();
        }
    }

    removeAds()

    setInterval(removeAds, 1000)
}

cleanWikiPage();

setTimeout(cleanWikiPage, 500)