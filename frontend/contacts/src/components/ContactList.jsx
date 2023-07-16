import React, { useState, useEffect } from 'react';

const ContactList = () => {
  const [isLoading, setIsLoading] = useState(true);
  const [contacts, setContacts] = useState([]);

  useEffect(() => {
    fetchContacts();
  }, []);

  const fetchContacts = async () => {
    const response = await fetch('https://localhost:5001/api/contacts', {
      method: 'GET',
      mode: 'cors',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    const data = await response.json();
    setContacts(data);
    setIsLoading(false);
  };

  return isLoading ? (
    <div>Loading...</div>
  ) : (
    <ul>
      {contacts.map((contact) => (
        <li key={contact.id}>{contact.fullName}</li>
      ))}
    </ul>
  );
};

export default ContactList;
