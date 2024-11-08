import DocCard from "@theme/DocCard";

export default function DocCardEx({ link }: {
    link: string
}) {
    const { metadata } = require("@site/docs" + link.replace(/^\/+/, "/"));
    return (
        <></>
    );
}